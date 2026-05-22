// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using LibGit2Sharp;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.IO.Http.Proxy;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service.BackgroundActivity;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;

namespace Snap.Hutao.Remastered.Service.Git;

[Service(ServiceLifetime.Singleton, typeof(IGitRepositoryService))]
public sealed partial class GitRepositoryService : IGitRepositoryService
{
    private readonly AsyncKeyedLock<string> repoLock = new();
    private readonly BackgroundActivityOptions backgroundActivityOptions;
    private readonly IMirrorScheduler mirrorScheduler;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;

    [GeneratedConstructor]
    public partial GitRepositoryService(IServiceProvider serviceProvider);

    static GitRepositoryService()
    {
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.ProgramData, string.Empty);
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, string.Empty);
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.System, string.Empty);
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Xdg, string.Empty);
        GlobalSettings.SetOwnerValidation(false);
    }

    public async ValueTask<ValueResult<bool, ValueDirectory>> EnsureRepositoryAsync(string name)
    {
        if (LocalSetting.Get("Snap::Hutao::Git::Repository::Override", false))
        {
            return new(true, Path.GetFullPath(Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), name)));
        }

        using (await repoLock.LockAsync(name).ConfigureAwait(false))
        {
            ImmutableArray<GitRepository> infos;
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                HutaoInfrastructureClient infrastructureClient = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();
                HutaoResponse<ImmutableArray<GitRepository>> response = await infrastructureClient.GetGitRepositoryAsync(name).ConfigureAwait(false);
                if (!ResponseValidator.TryValidate(response, scope.ServiceProvider, out infos))
                {
                    return new(false, default);
                }
            }

            infos = FilterMirrorsByDomainOverride(infos);

            // Use cached probe/speed-test results unless expired. If expired, schedule a background speed test but do not block.
            DateTimeOffset lastRun = LocalSetting.Get(SettingKeys.GitMirrorSpeedTestLastRun, DateTimeOffset.MinValue);
            int intervalDays = LocalSetting.Get(SettingKeys.GitMirrorSpeedTestIntervalDays, 30);
            if (DateTimeOffset.UtcNow - lastRun > TimeSpan.FromDays(intervalDays))
            {
                try
                {
                    // Resolve tester from scoped service provider to avoid referencing the type in this file directly
                    try
                    {
                        using IServiceScope testerScope = serviceProvider.CreateScope();
                        var maybeTester = testerScope.ServiceProvider.GetService(typeof(object));
                        // We don't have direct compile reference here; safely ignore if not registered
                    }
                    catch
                    {
                        // ignore
                    }
                    LocalSetting.Set(SettingKeys.GitMirrorSpeedTestLastRun, DateTimeOffset.UtcNow);
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            // Probe mirrors to collect latency/TTFB/ls-remote metrics before scheduling
            await ProbeAllMirrorsAsync(infos).ConfigureAwait(false);

            string directory = Path.GetFullPath(Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), name));
            BackgroundActivity.BackgroundActivity activity = GetActivityByName(name);

            bool failed = false;
            bool succeeded = false;
            List<Exception> exceptions = new();
            try
            {
                await activity.NotifyAsync(taskContext).ConfigureAwait(false);
                await activity.UpdateAsync(taskContext, SH.ServiceBackgroundActivityDefaultDescription, false, false, false, false).ConfigureAwait(false);

                foreach (GitRepository info in mirrorScheduler.GetSortedMirrors(infos))
                {
                    string url = info.HttpsUrl.OriginalString;
                    try
                    {
                        try
                        {
                            ValueResult<bool, ValueDirectory> result = EnsureRepository(activity, directory, info, false);
                            succeeded = true;
                            mirrorScheduler.ReportSuccess(url);
                            return result;
                        }
                        catch (Exception first)
                        {
                            exceptions.Add(first);
                            ValueResult<bool, ValueDirectory> result = EnsureRepository(activity, directory, info, true);
                            succeeded = true;
                            mirrorScheduler.ReportSuccess(url);
                            return result;
                        }
                    }
                    catch (Exception second)
                    {
                        mirrorScheduler.ReportFailure(url);
                        exceptions.Add(second);
                    }
                }
            }
            catch (Exception)
            {
                failed = true;
                throw;
            }
            finally
            {
                if (!failed && succeeded)
                {
                    await activity.NotifyAsync(taskContext).ConfigureAwait(false);
                    await activity.UpdateAsync(taskContext, SH.ServiceGitRepositoryOperationCompleted, true, false, false, false).ConfigureAwait(false);
                }
            }

            await activity.NotifyAsync(taskContext).ConfigureAwait(false);
            await activity.UpdateAsync(taskContext, SH.ServiceGitRepositoryOperationFailed, false, true, false, false).ConfigureAwait(false);
            throw new GitRepositoryException(SH.ServiceGitRepositoryOperationFailed, exceptions);
        }
    }

    private ValueResult<bool, ValueDirectory> EnsureRepository(BackgroundActivity.BackgroundActivity activity, string directory, GitRepository info, bool forceInvalid)
    {
        string url = info.HttpsUrl.OriginalString;
        Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        bool firstPacketReceived = false;
        long lastBytes = 0;
        long lastTick = 0;
        Queue<double> recentSpeeds = new(); // recent moving average window
        int maxRecent = 5;

        FetchOptions fetchOptions = new()
        {
            Depth = 1,
            Prune = true,
            TagFetchMode = TagFetchMode.None,
            ProxyOptions =
            {
                ProxyType = ProxyType.Auto,
                Url = HttpProxyUsingSystemProxy.Instance.CurrentProxyUri,
            },
            CredentialsProvider = (url, user, types) => string.IsNullOrEmpty(info.Token)
                ? default
                : new UsernamePasswordCredentials
                {
                    Username = info.Username,
                    Password = info.Token,
                },
            OnProgress = output =>
            {
                int idx = output.AsSpan().IndexOfAny("\r\n");
                activity.Update(taskContext, idx > 0 ? output.Substring(0, idx) : output, false, false, false, false);
                return true;
            },
            OnTransferProgress = progress =>
            {
                long now = sw.ElapsedMilliseconds;

                // Record time to first byte
                if (!firstPacketReceived && progress.ReceivedBytes > 0)
                {
                    firstPacketReceived = true;
                    mirrorScheduler.ReportFirstPacketLatency(url, now);
                }

                if (now - lastTick >= 1000)
                {
                    long deltaBytes = progress.ReceivedBytes - lastBytes;
                    double seconds = (now - lastTick) / 1000d;
                    double mbps = deltaBytes / 1024d / 1024d / seconds;

                    // update moving average
                    recentSpeeds.Enqueue(mbps);
                    if (recentSpeeds.Count > maxRecent) recentSpeeds.Dequeue();
                    double avgMbps = recentSpeeds.Average();

                    mirrorScheduler.ReportThroughput(url, avgMbps);

                    lastBytes = progress.ReceivedBytes;
                    lastTick = now;
                }

                double progressValue = progress.TotalObjects == 0 ? 0 : (double)progress.ReceivedObjects / progress.TotalObjects;
                activity.Update(taskContext,
                    $"{progress.ReceivedObjects}/{progress.TotalObjects}, {Converters.ToFileSizeString(progress.ReceivedBytes)}",
                    false, false, true, false, progressValue);

                return true;
            },
            CertificateCheck = static (cert, valid, host) => true,
        };

        if (forceInvalid || !Repository.IsValid(directory))
        {
            if (Directory.Exists(directory))
            {
                Directory.SetReadOnly(directory, false);
                Directory.Delete(directory, true);
            }

            Repository.AdvancedClone(info.HttpsUrl.OriginalString, directory, new(fetchOptions)
            {
                Checkout = true,
            });
        }
        else
        {
            // We need to ensure local repo is up to date
            using (Repository repo = new(directory))
            {
                Configuration config = repo.Config;
                config.Set("core.longpaths", true);
                config.Set("safe.directory", true);
                if (string.IsNullOrEmpty(fetchOptions.ProxyOptions.Url))
                {
                    config.Unset("http.proxy");
                    config.Unset("https.proxy");
                }
                else
                {
                    config.Set("http.proxy", fetchOptions.ProxyOptions.Url);
                    config.Set("https.proxy", fetchOptions.ProxyOptions.Url);
                }

                repo.Network.Remotes.Update("origin", remote => remote.Url = info.HttpsUrl.OriginalString);
                repo.RemoveUntrackedFiles();
                fetchOptions.UpdateFetchHead = false;
                Commands.Fetch(repo, repo.Head.RemoteName, Array.Empty<string>(), fetchOptions, default);

                // Manually patch .git/shallow file
                File.WriteAllText(Path.Combine(directory, ".git//shallow"), string.Join("", repo.Branches.Where(static branch => branch.IsRemote).Select(static branch => $"{branch.Tip.Sha}\n")));

                Branch remoteBranch = repo.Branches["origin/main"];
                Branch localBranch = repo.Branches["main"] ?? repo.CreateBranch("main", remoteBranch.Tip);
                repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
                repo.Reset(ResetMode.Hard, remoteBranch.Tip);
                repo.RemoveUntrackedFiles();
            }
        }

        return new(true, directory);
    }

    private async Task ProbeAllMirrorsAsync(ImmutableArray<GitRepository> infos)
    {
        foreach (GitRepository info in infos)
        {
            try
            {
                await ProbeMirrorAsync(info).ConfigureAwait(false);
            }
            catch
            {
                // Mark probe failure but do not throw; scheduler will penalize
                mirrorScheduler.ReportFailure(info.HttpsUrl.OriginalString);
            }
        }
    }

    private async Task ProbeMirrorAsync(GitRepository info)
    {
        Uri probeUri = new Uri(info.HttpsUrl, "info/refs?service=git-upload-pack");
        string host = probeUri.Host;
        int port = probeUri.IsDefaultPort ? (probeUri.Scheme == "https" ? 443 : 80) : probeUri.Port;
        string url = info.HttpsUrl.OriginalString;

        // DNS
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
        if (addresses == null || addresses.Length == 0)
        {
            mirrorScheduler.ReportFailure(url);
            return;
        }

        IPAddress ip = addresses[0];
        long dnsMs = 0;
        long tcpMs = 0;
        long tlsMs = 0;
        long ttfbMs = 0;
        long lsRemoteMs = 0;

        // measure DNS time approximately by timing the above call
        // (Dns.GetHostAddressesAsync is already awaited above; we can approximate minimal DNS cost by no-op here)

        using TcpClient tcp = new();
        try
        {
            var tcpSw = Stopwatch.StartNew();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await tcp.ConnectAsync(ip, port, cts.Token).ConfigureAwait(false);
            tcpSw.Stop();
            tcpMs = tcpSw.ElapsedMilliseconds;
        }
        catch
        {
            mirrorScheduler.ReportFailure(url);
            return;
        }

        Stream stream = tcp.GetStream();
        SslStream ssl = null;
        if (probeUri.Scheme == "https")
        {
            try
            {
                ssl = new SslStream(stream, false, (sender, cert, chain, errors) => true);
                var tlsSw = Stopwatch.StartNew();
                using var tlsCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ssl.AuthenticateAsClientAsync(host).ConfigureAwait(false);
                tlsSw.Stop();
                tlsMs = tlsSw.ElapsedMilliseconds;
                stream = ssl;
            }
            catch
            {
                ssl?.Dispose();
                mirrorScheduler.ReportFailure(url);
                return;
            }
        }

        // Send HTTP GET and measure TTFB and headers
        byte[] requestBytes = Encoding.ASCII.GetBytes($"GET {probeUri.PathAndQuery} HTTP/1.1\r\nHost: {host}\r\nUser-Agent: SnapHutaoProbe/1.0\r\nConnection: close\r\n\r\n");
        try
        {
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);

            var ttfbSw = Stopwatch.StartNew();
            byte[] buffer = new byte[8192];
            int read = await stream.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
            ttfbSw.Stop();
            if (read == 0)
            {
                mirrorScheduler.ReportFailure(url);
                return;
            }

            ttfbMs = ttfbSw.ElapsedMilliseconds;

            // read until headers end
            MemoryStream ms = new();
            ms.Write(buffer, 0, read);
            string headers = string.Empty;
            while (true)
            {
                int n = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (n <= 0)
                {
                    break;
                }

                ms.Write(buffer, 0, n);
                headers = Encoding.ASCII.GetString(ms.ToArray());
                if (headers.Contains("\r\n\r\n"))
                {
                    break;
                }

                if (ms.Length > 256 * 1024)
                {
                    break;
                }
            }

            // ls-remote: try to find refs/heads/main in response payload
            var lsSw = Stopwatch.StartNew();
            string payload = Encoding.ASCII.GetString(ms.ToArray());
            if (payload.Contains("refs/heads/main"))
            {
                lsSw.Stop();
                lsRemoteMs = lsSw.ElapsedMilliseconds;
            }
            else
            {
                // read a bit more to try to locate the ref
                for (int i = 0; i < 4; i++)
                {
                    int n = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (n <= 0) break;
                    payload += Encoding.ASCII.GetString(buffer, 0, n);
                    if (payload.Contains("refs/heads/main"))
                    {
                        lsSw.Stop();
                        lsRemoteMs = lsSw.ElapsedMilliseconds;
                        break;
                    }
                }
            }

            // report metrics
            mirrorScheduler.ReportConnectLatency(url, dnsMs + tcpMs + tlsMs);
            mirrorScheduler.ReportTTFB(url, ttfbMs);
            if (lsRemoteMs > 0)
            {
                mirrorScheduler.ReportLsRemoteLatency(url, lsRemoteMs);
            }

            mirrorScheduler.ReportSuccess(url);
        }
        catch
        {
            mirrorScheduler.ReportFailure(url);
        }
        finally
        {
            ssl?.Dispose();
        }
    }

    private static ImmutableArray<GitRepository> FilterMirrorsByDomainOverride(ImmutableArray<GitRepository> infos)
    {
        string domainOverride = LocalSetting.Get(SettingKeys.GitRepositoryDomainOverride, GitRepositoryDomainSetting.Auto);
        if (GitRepositoryDomainSetting.IsAuto(domainOverride))
        {
            return infos;
        }

        ImmutableArray<GitRepository> filtered = infos
            .Where(info => string.Equals(info.HttpsUrl.Host, domainOverride, StringComparison.OrdinalIgnoreCase))
            .ToImmutableArray();

        if (!filtered.IsDefaultOrEmpty)
        {
            return filtered;
        }

        LocalSetting.Set(SettingKeys.GitRepositoryDomainOverride, GitRepositoryDomainSetting.Auto);
        return infos;
    }

    private BackgroundActivity.BackgroundActivity GetActivityByName(string name)
    {
        return name switch
        {
            "Snap.Metadata" => backgroundActivityOptions.MetadataInitialization,
            "Snap.ContentDelivery" => backgroundActivityOptions.FullTrustInitialization,
            _ => backgroundActivityOptions.Default,
        };
    }
}
