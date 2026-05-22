// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Snap.Hutao.Remastered.Core.IO.Http.Proxy;
using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Snap.Hutao.Remastered.Service.Git;

[Service(ServiceLifetime.Scoped)]
internal sealed class GitMirrorSpeedTester
{
    private readonly ILogger<GitMirrorSpeedTester> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly IMirrorScheduler mirrorScheduler;

    // Limit concurrency to avoid too many parallel downloads
    private const int MaxConcurrency = 4;

    // Path to test file relative to mirror root
    private const string TestFileRelativePath = "Snap.Metadata/Genshin/EN/Material.json";

    public GitMirrorSpeedTester(ILogger<GitMirrorSpeedTester> logger, IServiceProvider serviceProvider, ITaskContext taskContext, IMirrorScheduler mirrorScheduler)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.taskContext = taskContext;
        this.mirrorScheduler = mirrorScheduler;
    }

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
                    logger.LogWarning(ex, "Git mirror speed test failed: {Url}", mirror.HttpsUrl.OriginalString);
                    mirrorScheduler.ReportFailure(mirror.HttpsUrl.OriginalString);
                }
                finally
                {
                    sem.Release();
                }
            }, token));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task TestMirrorAsync(GitRepository mirror, CancellationToken token)
    {
        string url = mirror.HttpsUrl.OriginalString.TrimEnd('/') + "/" + TestFileRelativePath;
        logger.LogInformation("[SpeedTest] Testing mirror: {Url}", url);

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
                logger.LogWarning("[SpeedTest] Mirror returned non-success status: {Status} for {Url}", response.StatusCode, url);
                mirrorScheduler.ReportFailure(mirror.HttpsUrl.OriginalString);
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

            logger.LogInformation("[SpeedTest] Mirror {Url} downloaded {Bytes} bytes in {Seconds}s -> {Mbps} MB/s", url, total, seconds, mbps);

            mirrorScheduler.ReportThroughput(mirror.HttpsUrl.OriginalString, mbps);
            mirrorScheduler.ReportSuccess(mirror.HttpsUrl.OriginalString);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[SpeedTest] Mirror test failed for {Url}", url);
            mirrorScheduler.ReportFailure(mirror.HttpsUrl.OriginalString);
        }
    }
}
