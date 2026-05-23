// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class SettingNetViewModel : Abstraction.ViewModel
{
    private static readonly NameValue<string> AutoDomainOption = new(SH.ViewPageSettingGitRepositoryDomainLockModeAuto, GitRepositoryDomainSetting.Auto);

    private readonly HutaoInfrastructureClient hutaoInfrastructureClient;
    private readonly IServiceProvider serviceProvider;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly ITaskContext taskContext;
    private readonly GitMirrorSelectionService mirrorSelectionService;

    public AppOptions AppOptions { get; }

    public SettingNetViewModel(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        hutaoInfrastructureClient = serviceProvider.GetRequiredService<HutaoInfrastructureClient>();
        contentDialogFactory = serviceProvider.GetRequiredService<IContentDialogFactory>();
        taskContext = serviceProvider.GetRequiredService<ITaskContext>();
        mirrorSelectionService = serviceProvider.GetRequiredService<GitMirrorSelectionService>();
        AppOptions = serviceProvider.GetRequiredService<AppOptions>();
    }

    /// <summary>
    /// 获取或设置 Git 仓库域名选项列表
    /// Get or set the Git repository domain options list
    /// 
    /// 初始值为仅包含 Auto 选项，调用 InitializeGitRepositoryDomainOptionsAsync 后会更新为完整列表
    /// Initial value contains only Auto option, will be updated to full list after calling InitializeGitRepositoryDomainOptionsAsync
    /// </summary>
    public ImmutableArray<NameValue<string>> GitRepositoryDomainOptions
    {
        get => field == default ? [AutoDomainOption] : field;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 获取或设置当前选中的 Git 仓库域名选项
    /// Get or set the currently selected Git repository domain option
    /// 
    /// Getter 逻辑 / Getter logic:
    /// 1. 从配置中读取已保存的域名值 (AppOptions.GitRepositoryDomainOverride.Value)
    ///    Read the saved domain value from configuration
    /// 2. 如果首次访问（field 为 null），在选项列表中查找该域名对应的选项
    ///    If first access (field is null), find the corresponding option in the list
    /// 3. 如果找不到，则默认返回 AutoDomainOption
    ///    If not found, return AutoDomainOption as default
    /// 
    /// Setter 逻辑 / Setter logic:
    /// 1. 调用 SetProperty 更新字段值
    ///    Call SetProperty to update the field
    /// 2. 同时将选中值的 Value 属性保存到配置中，供后续应用启动时读取
    ///    Also save the Value property to AppOptions so it persists across app restarts
    /// </summary>
    // TODO: Replace with IObservableProperty
    public NameValue<string>? SelectedGitRepositoryDomainOption
    {
        get
        {
            string selectedDomain = AppOptions.GitRepositoryDomainOverride.Value;
            return field ??= FindDomainOption(GitRepositoryDomainOptions, selectedDomain) ?? AutoDomainOption;
        }
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.GitRepositoryDomainOverride.Value = value.Value;
            }
        }
    }

    /// <summary>
    /// 异步初始化 Git 仓库域名选项
    /// Asynchronously initialize Git repository domain options
    /// 
    /// 流程 / Process:
    /// 1. 从 API 获取可用的域名选项列表
    ///    Fetch available domain options from API
    /// 2. 从配置中读取上次保存的域名 (AppOptions.GitRepositoryDomainOverride.Value)
    ///    Read the previously saved domain from configuration
    /// 3. 在选项列表中查找该域名：
    ///    Try to find that domain in the options list:
    ///    - 如果找到，则选中该选项 / If found, select that option
    ///    - 如果找不到（例如 API 不再提供该源），则重置为 Auto
    ///      If not found (e.g., source no longer available), reset to Auto
    /// 4. 将初始化后的选项列表和选中值保存到属性中
    ///    Save the initialized options and selected value to properties
    /// </summary>
    public async ValueTask InitializeGitRepositoryDomainOptionsAsync(CancellationToken token)
    {
        // 从 API 获取完整的域名选项列表（包含带主机名显示的 Auto 选项）
        // Fetch complete domain options from API (includes Auto option with host display)
        ImmutableArray<NameValue<string>> options = await GetDomainOptionsFromApiAsync(token).ConfigureAwait(false);
        await taskContext.SwitchToMainThreadAsync();

        // 读取配置中保存的域名设置
        // Read the saved domain setting from configuration
        string configuredDomain = AppOptions.GitRepositoryDomainOverride.Value;

        // 尝试在选项列表中找到配置的域名对应的选项对象
        // Try to find the option object corresponding to the configured domain
        NameValue<string>? selected = FindDomainOption(options, configuredDomain);

        // 如果配置的域名在当前选项列表中找不到（例如 API 返回的源列表已改变）
        // If the configured domain is not found in current options (e.g., API sources changed)
        if (selected is null)
        {
            // 重置为 Auto 并保存到配置
            // Reset to Auto and save to configuration
            configuredDomain = GitRepositoryDomainSetting.Auto;
            AppOptions.GitRepositoryDomainOverride.Value = configuredDomain;

            // 选中列表的第一个选项（即 Auto 选项）
            // Select the first option in the list (the Auto option)
            selected = options.First();
        }

        // 更新 UI 绑定的选项列表和选中值
        // Update the options list and selected value for UI binding
        GitRepositoryDomainOptions = options;
        SelectedGitRepositoryDomainOption = selected;
    }

    [Command("OpenGitSpeedTestDialogCommand")]
    private async Task OpenGitSpeedTestDialogAsync()
    {
        GitSourcesSpeedTestDialog dialog = await contentDialogFactory.CreateInstanceAsync<GitSourcesSpeedTestDialog>(serviceProvider).ConfigureAwait(false);
        await dialog.ShowAsync();
    }

    /// <summary>
    /// 从 API 获取 Git 仓库域名选项列表
    /// Fetch Git repository domain options from API
    /// 
    /// 
    /// 流程 / Process:
    /// 1. 从两个仓库（Snap.Metadata、Snap.ContentDelivery）获取主机名列表
    ///    Fetch host names from two repositories
    /// 2. 获取系统当前实际判断的 Auto 源（通过 MirrorScheduler）用于 Auto 选项的显示
    ///    Get the actual Auto source determined by the system for Auto option display
    /// 3. 将所有主机名按字母顺序排序并返回，Auto 选项始终为第一个
    ///    Sort all hosts alphabetically and return, Auto option is always first
    /// </summary>
    /// <returns>
    /// 返回格式 / Return format:
    /// [
    ///   Auto (github.com),          // Auto 选项 + 系统实际判断使用的源
    ///   gitee.com,
    ///   github.com,
    ///   中国 - 华中,
    ///   ...
    /// ]
    /// 
    /// </returns>
    private async ValueTask<ImmutableArray<NameValue<string>>> GetDomainOptionsFromApiAsync(CancellationToken token)
    {
        // 用于存储所有唯一的选项（不区分大小写）
        // Stores all unique option values (case-insensitive)
        ImmutableHashSet<string>.Builder optionValues = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> displayMap = new(StringComparer.OrdinalIgnoreCase);

        // 从两个仓库获取主机名
        // Get host names from both repositories
        await AddFriendlyNameFromRepositoryNameAsync("Snap.Metadata", optionValues, displayMap, token).ConfigureAwait(false);
        await AddFriendlyNameFromRepositoryNameAsync("Snap.ContentDelivery", optionValues, displayMap, token).ConfigureAwait(false);

        // 获取系统当前实际选择的 Auto 源
        // Get the actual Auto source that the system currently determined
        string? actualAutoSource = await GetActualAutoSourceDisplayNameAsync(optionValues, displayMap, token).ConfigureAwait(false);

        // 创建 Auto 选项：如果获取到实际源则显示为 "Auto (源)"，否则仅显示 "Auto"
        // Create Auto option: display as "Auto (source)" if actual source found, otherwise just "Auto"
        NameValue<string> autoOption = actualAutoSource is not null
            ? new NameValue<string>($"{SH.ViewPageSettingGitRepositoryDomainLockModeAuto} ({actualAutoSource})", GitRepositoryDomainSetting.Auto)
            : AutoDomainOption;

        // 返回：Auto 选项 + 其他主机名（按字母顺序排序）
        // Return: Auto option + other hosts (sorted alphabetically)
        return [autoOption, .. optionValues
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .Select(value => new NameValue<string>(displayMap[value], value))];
    }

    /// <summary>
    /// 从指定仓库获取友好名称或主机，并添加到主机列表中
    /// Get friendly names or hosts from specified repository and add to hosts list
    /// 
    /// 参数 / Parameters:
    /// - repositoryName: 仓库名称（如 "Snap.Metadata"）/ Repository name
    /// - hosts: 主机名集合 / Host names collection
    /// - token: 取消令牌 / Cancellation token
    /// 
    /// 返回 / Returns:
    /// 该仓库中第一个有效的主机名（用于 Auto 选项显示）
    /// First valid host name from this repository (for Auto option display)
    /// 
    /// 逻辑 / Logic:
    /// 1. 调用 API 获取仓库列表
    ///    Call API to fetch repository list
    /// 2. 对每个仓库：
    ///    For each repository:
    ///    - 如果有 FriendlyName（本地化名称），则获取对应的本地化字符串
    ///      If has FriendlyName, fetch corresponding localized string
    ///    - 否则使用 HTTPS URL 的主机名（如 github.com）
    ///      Otherwise use host from HTTPS URL
    /// 3. 将所有有效的主机名添加到集合中
    ///    Add all valid host names to collection
    /// 4. 返回第一个有效的主机名
    ///    Return first valid host name
    /// </summary>
    private async ValueTask<string?> AddFriendlyNameFromRepositoryNameAsync(
        string repositoryName,
        ImmutableHashSet<string>.Builder optionValues,
        Dictionary<string, string> displayMap,
        CancellationToken token)
    {
        // 调用 API 获取该仓库的配置列表
        // Call API to get the repository configuration list
        Web.Hutao.Response.HutaoResponse<ImmutableArray<GitRepository>> response = await hutaoInfrastructureClient.GetGitRepositoryAsync(repositoryName, token).ConfigureAwait(false);
        ImmutableArray<GitRepository> repositories = response.Data;

        // 如果 API 调用失败或返回空列表，则返回 null
        // Return null if API call failed or returned empty list
        if (response.ReturnCode != 0 || repositories.IsDefaultOrEmpty)
        {
            return null;
        }

        // 用于返回第一个有效的主机名
        // Stores the first valid host name to return
        string? firstDisplayName = null;

        // 遍历所有仓库配置
        // Iterate through all repository configurations
        foreach (GitRepository repository in repositories)
        {
            string? displayName = null;
            string? optionValue = null;

            // 优先使用本地化的友好名称
            // Prefer localized friendly name
            if (!string.IsNullOrWhiteSpace(repository.FriendlyName))
            {
                displayName = SH.GetString("ViewModelSettingNetGitFriendlyName" + repository.FriendlyName);
                optionValue = repository.FriendlyName;
            }
            else
            {
                displayName = repository.HttpsUrl.Host;
                optionValue = repository.HttpsUrl.Host;
            }

            // 如果成功获取了主机名
            // If successfully got a host
            if (!string.IsNullOrWhiteSpace(optionValue) && displayName is not null)
            {
                if (optionValues.Add(optionValue))
                {
                    displayMap[optionValue] = displayName;
                }

                firstDisplayName ??= displayName;
            }
        }

        // 返回第一个有效的主机名，用于显示在 Auto 选项中
        // Return first valid host to display in Auto option
        return firstDisplayName;
    }

    /// <summary>
    /// 获取系统当前实际选择的 Auto 源的显示名称
    /// Get the display name of the actual Auto source currently determined by the system
    /// 
    /// 流程 / Process:
    /// 1. 从配置中读取当前用户保存的源（可能是 Auto 或具体的源）
    ///    Read the currently saved source from configuration
    /// 2. 如果配置的源是 Auto：
    ///    If the configured source is Auto:
    ///    - 尝试从缓存获取系统上次测试确定的最优源
    ///      Try to get the optimal source from cache
    ///    - 在 displayMap 中查找该源的显示名称
    ///      Find the display name of that source
    /// 3. 返回找到的显示名称
    ///    Return the found display name
    /// </summary>
    private async ValueTask<string?> GetActualAutoSourceDisplayNameAsync(
        ImmutableHashSet<string>.Builder optionValues,
        Dictionary<string, string> displayMap,
        CancellationToken token)
    {
        try
        {
            // 读取当前配置的源
            // Read the currently configured source
            string configuredDomain = AppOptions.GitRepositoryDomainOverride.Value;

            // 如果用户选择的不是 Auto，则不需要显示实际源
            // If the user didn't select Auto, no need to show actual source
            if (!GitRepositoryDomainSetting.IsAuto(configuredDomain))
            {
                return null;
            }

            // 尝试获取系统确定的最优源
            // Try to get the system-determined optimal source
            string? optimalMirror = await mirrorSelectionService.GetOptimalMirrorAsync(token).ConfigureAwait(false);

            // 如果获取到最优源，尝试在 displayMap 中查找其显示名称
            // If got optimal mirror, try to find its display name
            if (!string.IsNullOrWhiteSpace(optimalMirror))
            {
                if (displayMap.TryGetValue(optimalMirror, out string? displayName))
                {
                    return displayName;
                }

                // 如果 displayMap 中找不到，尝试直接使用 URL 的主机名
                // If not found in displayMap, try to use the host from URL directly
                if (Uri.TryCreate(optimalMirror, UriKind.Absolute, out Uri? uri))
                {
                    return uri.Host;
                }
            }

            return null;
        }
        catch
        {
            // 如果出错，返回 null，不显示实际源
            // If error, return null, don't show actual source
            return null;
        }
    }

    /// <summary>
    /// 在选项列表中查找指定域名的选项
    /// Find the option corresponding to the specified domain in the options list
    /// 
    /// 参数 / Parameters:
    /// - options: 选项列表 / Options list
    /// - domain: 要查找的域名值（如 "github.com" 或 "Auto"）/ Domain value to find
    /// 
    /// 返回 / Returns:
    /// 匹配的 NameValue 对象，或 null 如果未找到 / Matching NameValue object, or null if not found
    /// 
    /// 说明 / Notes:
    /// 比较时不区分大小写，因为不同来源的域名可能大小写不同
    /// Comparison is case-insensitive because domains from different sources may have different casing
    /// </summary>
    private static NameValue<string>? FindDomainOption(ImmutableArray<NameValue<string>> options, string domain)
    {
        foreach (NameValue<string> option in options)
        {
            // 使用不区分大小写的比较（StringComparison.OrdinalIgnoreCase）
            // Use case-insensitive comparison
            if (string.Equals(option.Value, domain, StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }

        return default;
    }
}
