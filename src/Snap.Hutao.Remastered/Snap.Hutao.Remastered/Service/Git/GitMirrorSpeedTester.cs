// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO.Http.Proxy;
using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Git;

[Service(ServiceLifetime.Scoped)]
internal sealed class GitMirrorSpeedTester
{
    private readonly ILogger<GitMirrorSpeedTester> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly IMirrorScheduler mirrorScheduler;

    // Limit concurrency to avoid too many parallel downloads
    private const int MaxConcurrency = 10;

    // Repository name for speed testing
    private const string TestRepositoryName = "Snap.GitTest";

    // Path to test file in the test repository (using raw content path)
    private const string TestFilePath = "st.png";

    public GitMirrorSpeedTester(ILogger<GitMirrorSpeedTester> logger, IServiceProvider serviceProvider, ITaskContext taskContext, IMirrorScheduler mirrorScheduler)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.taskContext = taskContext;
        this.mirrorScheduler = mirrorScheduler;
    }

    /// <summary>
    /// Run a single round of speed tests for the given mirrors. This method can be called periodically by the scheduler.
    /// </summary>
    /// <param name="mirrors">需要测试的镜像源列表</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task RunOnceAsync(ImmutableArray<GitRepository> mirrors, CancellationToken token)
    {
        using SemaphoreSlim sem = new(MaxConcurrency);
        List<Task> tasks = new();

        foreach (GitRepository mirror in mirrors)
        {
            await sem.WaitAsync(token).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await TestMirrorAsync(mirror, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    string mirrorIdentifier = GetMirrorIdentifier(mirror);
                    logger.LogWarning(ex, "Git mirror speed test failed: {Identifier}", mirrorIdentifier);
                    mirrorScheduler.ReportFailure(mirrorIdentifier);
                }
                finally
                {
                    sem.Release();
                }
            }, token));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Test the speed of a single mirror by downloading the test file and measuring the time taken. Report the result to the scheduler.
    /// </summary>
    /// <param name="mirror">要测试的镜像源</param>
    /// <param name="token">取消令牌</param>
    /// <returns></returns>
    private async Task TestMirrorAsync(GitRepository mirror, CancellationToken token)
    {
        string mirrorIdentifier = GetMirrorIdentifier(mirror);

        // Extract the organization/user from the mirror URL and construct the test repository URL
        Uri mirrorUri = mirror.HttpsUrl;
        string scheme = mirrorUri.Scheme; // http / https
        string host = mirrorUri.Host;

        // Handle non-standard ports (Port returns -1 for standard ports)
        string hostWithPort = mirrorUri.Port != -1 ? $"{host}:{mirrorUri.Port}" : host;

        // Construct URL to Snap.GitTest repository, preserving the original scheme and port
        // Example: https://github.com/SnapHutaoRemasteringProject/Snap.GitTest/raw/main/st.png
        // Or: http://cnswgit.snaphutaorp.org:12345/SnapHutaoRemasteringProject/Snap.GitTest/raw/main/st.png
        string organization = GetOrganizationFromUri(mirrorUri);
        string url = $"{scheme}://{hostWithPort}/{organization}/{TestRepositoryName}/raw/main/{TestFilePath}";

        logger.LogInformation("[SpeedTest] Testing mirror: {Identifier} - {Url}", mirrorIdentifier, url);

        using SocketsHttpHandler handler = new()
        {
            UseProxy = true,
            Proxy = HttpProxyUsingSystemProxy.Instance,
            ConnectTimeout = TimeSpan.FromSeconds(5),
        };

        using HttpClient client = new(handler)
        {
            Timeout = TimeSpan.FromSeconds(20),
        };

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(mirror.Token))
            {
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{mirror.Username}:{mirror.Token}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }

            Stopwatch sw = Stopwatch.StartNew();
            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[SpeedTest] Mirror returned non-success status: {Status} for {Identifier}", response.StatusCode, mirrorIdentifier);
                mirrorScheduler.ReportFailure(mirrorIdentifier);
                return;
            }

            // Read the content stream and measure throughput
            using Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            byte[] buffer = new byte[8192];
            long total = 0;
            Stopwatch readSw = Stopwatch.StartNew();
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
            {
                total += read;
            }
            readSw.Stop();

            double seconds = Math.Max(0.001, readSw.Elapsed.TotalSeconds);
            double mbps = (total / 1024d / 1024d) / seconds;

            logger.LogInformation("[SpeedTest] Mirror {Identifier} downloaded {Bytes} bytes in {Seconds}s -> {Mbps} MB/s", mirrorIdentifier, total, seconds, mbps);

            mirrorScheduler.ReportThroughput(mirrorIdentifier, mbps);
            mirrorScheduler.ReportSuccess(mirrorIdentifier);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[SpeedTest] Mirror test failed for {Identifier}", mirrorIdentifier);
            mirrorScheduler.ReportFailure(mirrorIdentifier);
        }
    }

    /// <summary>
    /// 获取镜像源标识符（FriendlyName 或域名）。
    /// </summary>
    private static string GetMirrorIdentifier(GitRepository mirror)
    {
        return !string.IsNullOrWhiteSpace(mirror.FriendlyName)
            ? mirror.FriendlyName
            : mirror.HttpsUrl.Host;
    }

    /// <summary>
    /// Extract the organization or user name from a Git repository URI.
    /// For GitHub URLs like https://github.com/SnapHutaoRemasteringProject/Snap.Metadata.git,
    /// </summary>
    /// <returns>
    /// "SnapHutaoRemasteringProject".
    /// </returns>
    private static string GetOrganizationFromUri(Uri repositoryUri)
    {
        string path = repositoryUri.AbsolutePath.Trim('/');
        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length > 0)
        {
            return segments[0];
        }

        throw new InvalidOperationException($"Unable to extract organization from repository URI: {repositoryUri}");
    }
}
