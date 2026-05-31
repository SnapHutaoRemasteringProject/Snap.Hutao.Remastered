// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Snap.Hutao.Remastered.Service.Git;

/// <summary>
/// Git 镜像源选择服务
/// Git Mirror Selection Service
/// 
/// 负责：
/// 1. 从 API 获取可用的镜像源列表
/// 2. 测试镜像源的速度和可用性
/// 3. 选择最优的镜像源
/// 4. 管理缓存（测试时间、源列表哈希）
/// 5. 根据过期时间或源列表改变触发重新测试
/// </summary>
[Service(ServiceLifetime.Scoped)]
public sealed class GitMirrorSelectionService
{
    private static int isInitializing;

    /// <summary>
    /// 镜像源测试间隔（天数）
    /// Interval in days to retest mirror sources
    /// </summary>
    private const int TestIntervalDays = 45;

    private readonly HutaoInfrastructureClient hutaoInfrastructureClient;
    private readonly AppOptions appOptions;
    private readonly ITaskContext taskContext;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<GitMirrorSelectionService> logger;

    public GitMirrorSelectionService(
        HutaoInfrastructureClient hutaoInfrastructureClient,
        AppOptions appOptions,
        ITaskContext taskContext,
        IServiceProvider serviceProvider,
        ILogger<GitMirrorSelectionService> logger)
    {
        this.hutaoInfrastructureClient = hutaoInfrastructureClient;
        this.appOptions = appOptions;
        this.taskContext = taskContext;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <summary>
    /// 获取最优的镜像源
    /// Get the optimal mirror source
    /// 
    /// 流程：
    /// 1. 检查是否需要重新测试
    /// 2. 如果需要，则执行测试并选择最优源
    /// 3. 返回最优源的 URL
    /// </summary>
    public async ValueTask<string?> GetOptimalMirrorAsync(bool allowTest, CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref isInitializing, 1, 0) is not 0)
        {
            return null;
        }

        try
        {
            return await GetOptimalMirrorFromRepositoryFlowAsync(allowTest, token).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref isInitializing, 0);
        }
    }

    private async ValueTask<string?> GetOptimalMirrorFromRepositoryFlowAsync(bool allowTest, CancellationToken token)
    {
        // 根据我们设置的标准来检查是否需要重新测试
        if (await ShouldRefreshMirrorAsync(token).ConfigureAwait(false))
        {
            // 需要重新测试
            if (allowTest)
            {
                return await TestAndSelectBestMirrorAsync(token).ConfigureAwait(false);
            }
            // 如果不允许测试（例如为了防止阻塞UI），跳过测试，继续往下尝试从缓存中读取
        }
        // 理论上下面这部分代码应该删除，这属于必须测试的逻辑范围

        // 返回当前配置的域名（可能是 Auto 或之前选择的最优源）
        string domainOverride = appOptions.GitRepositoryDomainOverride.Value;

        // 如果用户手动指定了具体的源，直接返回
        if (!GitRepositoryDomainSetting.IsAuto(domainOverride))
        {
            return domainOverride;
        }

        // 如果是 Auto 模式，尝试从缓存中获取上次测试的最优源
        string cachedOptimal = appOptions.GitRepositoryDomainCachedOptimal.Value;
        if (!string.IsNullOrWhiteSpace(cachedOptimal))
        {
            return cachedOptimal;
        }

        // 没有缓存的最优源，返回 null
        return null;
    }

    /// <summary>
    /// 测试所有镜像源并选择最优的
    /// Test all mirror sources and select the best one
    /// 
    /// 流程：
    /// 1. 从 API 获取所有镜像源
    /// 2. 使用 GitMirrorSpeedTester 进行速度测试
    /// 3. 从测试结果中选择最优源
    /// 4. 保存选择结果和测试时间
    /// </summary>
    /// <returns>
    /// The URL of the optimal mirror source, or null if none is found.
    /// </returns>
    public async ValueTask<string?> TestAndSelectBestMirrorAsync(CancellationToken token)
    {
        try
        {
            // 获取所有镜像源
            ImmutableArray<GitRepository> repositories = await GetAllMirrorsAsync(token).ConfigureAwait(false);
            if (repositories.IsDefaultOrEmpty)
            {
                return null;
            }

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                // 执行速度测试核心逻辑
                GitMirrorSpeedTester? tester = scope.ServiceProvider.GetService<GitMirrorSpeedTester>();
                if (tester is not null)
                {
                    await tester.RunOnceAsync(repositories, token).ConfigureAwait(false);
                }

                // 提取唯一的镜像标识符（FriendlyName 或域名）
                // 这样避免重复存储 Snap.Metadata 和 Snap.ContentDelivery 的相同镜像源
                HashSet<string> uniqueMirrorIdentifiers = new HashSet<string>(
                    repositories.AsEnumerable().Select(repo => GetMirrorKey(repo)),
                    StringComparer.OrdinalIgnoreCase);

                // 获取排序后的镜像源标识符列表
                IMirrorScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMirrorScheduler>();
                IReadOnlyList<string> sortedMirrorIdentifiers = scheduler.GetSortedMirrors(uniqueMirrorIdentifiers);
                if (sortedMirrorIdentifiers.Count == 0)
                {
                    return null;
                }

                // 选择评分最高的镜像源标识符
                string bestMirrorKey = sortedMirrorIdentifiers[0];

                // 保存测试结果到缓存（不影响用户的手动选择）
                await taskContext.SwitchToMainThreadAsync();
                appOptions.GitRepositoryDomainCachedOptimal.Value = bestMirrorKey;
                appOptions.GitMirrorLastTestTimeUtc.Value = DateTime.UtcNow.ToString("O");
                appOptions.GitMirrorSourcesHash.Value = GetSourcesHash(repositories);

                return bestMirrorKey;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to test and select best Git mirror");
            return null;
        }
    }

    /// <summary>
    /// 检查是否需要重新测试镜像源
    /// Check if mirror sources need to be retested
    /// 
    /// 条件：
    /// 1. 从未测试过
    /// 2. 距离上次测试超过 30 天
    /// 3. API 返回的源列表哈希值改变（表示源列表更新了）
    /// </summary>
    /// <returns>
    /// bool: represent whether should retest all repos
    /// </returns>
    public async ValueTask<bool> ShouldRefreshMirrorAsync(CancellationToken token)
    {
        // 如果已经手动指定了源，则不需要自动测试（该选项非auto）
        if (!GitRepositoryDomainSetting.IsAuto(appOptions.GitRepositoryDomainOverride.Value))
        {
            return false;
        }

        // 检查是否曾经测试过
        string lastTestTimeStr = appOptions.GitMirrorLastTestTimeUtc.Value;
        if (string.IsNullOrWhiteSpace(lastTestTimeStr))
        {
            return true; // 从未测试过，需要测试
        }

        // 检查测试时间是否超过 30 天
        if (DateTime.TryParseExact(lastTestTimeStr, "O", null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastTestTime))
        {
            if (DateTime.UtcNow - lastTestTime > TimeSpan.FromDays(TestIntervalDays))
            {
                return true; // 超过 30 天，需要重新测试
            }
        }

        // 检查源列表是否改变
        try
        {
            ImmutableArray<GitRepository> currentRepositories = await GetAllMirrorsAsync(token).ConfigureAwait(false);
            string currentHash = GetSourcesHash(currentRepositories);
            string savedHash = appOptions.GitMirrorSourcesHash.Value;

            if (currentHash != savedHash)
            {
                return true; // 源列表改变了，需要重新测试
            }
        }
        catch
        {
            // 如果获取源列表失败，暂时不重新测试
        }

        return false; // 不需要重新测试
    }

    /// <summary>
    /// 从 API 获取所有镜像源
    /// Get all mirror sources from API
    /// </summary>
    private async ValueTask<ImmutableArray<GitRepository>> GetAllMirrorsAsync(CancellationToken token)
    {
        ImmutableArray<GitRepository>.Builder repositories = ImmutableArray.CreateBuilder<GitRepository>();

        // 从 Snap.Metadata 获取
        HutaoResponse<ImmutableArray<GitRepository>> response = await hutaoInfrastructureClient
            .GetGitRepositoryAsync("Snap.Metadata", token)
            .ConfigureAwait(false);

        if (response.Data.Length > 0)
        {
            repositories.AddRange(response.Data);
        }

        // 从 Snap.ContentDelivery 获取
        response = await hutaoInfrastructureClient
            .GetGitRepositoryAsync("Snap.ContentDelivery", token)
            .ConfigureAwait(false);

        if (response.Data.Length > 0)
        {
            repositories.AddRange(response.Data);
        }

        return repositories.ToImmutable();
    }

    /// <summary>
    /// 计算源列表的哈希值
    /// Calculate hash of mirror sources to detect changes
    /// </summary>
    private static string GetSourcesHash(ImmutableArray<GitRepository> repositories)
    {
        if (repositories.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        // 构建源列表的字符串表示
        StringBuilder sb = new();
        foreach (GitRepository repo in repositories)
        {
            sb.Append(repo.HttpsUrl.OriginalString);
            sb.Append("|");
        }

        // 计算哈希
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// 获取镜像源的唯一标识符（FriendlyName 或域名）。
    /// </summary>
    private static string GetMirrorKey(GitRepository repository)
    {
        return !string.IsNullOrWhiteSpace(repository.FriendlyName)
            ? repository.FriendlyName
            : repository.HttpsUrl.Host;
    }
}
