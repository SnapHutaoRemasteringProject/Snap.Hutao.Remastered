// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using LibGit2Sharp;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.IO.Http.Proxy;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service.BackgroundActivity;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Service.FileUnlocker;
using System.Collections.Immutable;
using System.IO;
using System.Diagnostics;

namespace Snap.Hutao.Remastered.Service.Git;

[Service(ServiceLifetime.Singleton, typeof(IGitRepositoryService))]
public sealed partial class GitRepositoryService : IGitRepositoryService
{
    private readonly AsyncKeyedLock<string> repoLock = new();
    private readonly BackgroundActivityOptions backgroundActivityOptions;
    private readonly IFileUnlockerService fileUnlockerService;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly RepositoryCatalogService repositoryCatalogService;

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

    public async ValueTask<ValueResult<bool, ValueDirectory>> EnsureRepositoryAsync(string name, CancellationToken token = default)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        token = linked.Token;
        await taskContext.SwitchToBackgroundAsync();
        token.ThrowIfCancellationRequested();
        if (LocalSetting.Get("Snap::Hutao::Git::Repository::Override", false))
        {
            return new(true, Path.GetFullPath(Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), name)));
        }

        using (await repoLock.LockAsync(name).ConfigureAwait(false))
        {
            ImmutableArray<GitRepository> infos = await repositoryCatalogService.GetRepositoriesAsync(name, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            string directory = Path.GetFullPath(Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), name));

            BackgroundActivity.BackgroundActivity activity = GetActivityByName(name);

            if (activity == backgroundActivityOptions.FullTrustInitialization)
            {
                // Unlock files in the target directory before git operations to prevent
                // file-in-use errors during delete/overwrite.
                // https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.FileUnlocker
                // await fileUnlockerService.UnlockAsync(directory).ConfigureAwait(false);
            }

            bool failed = false;
            bool succeeded = false;
            List<Exception> exceptions = [];
            try
            {
                await activity.NotifyAsync(taskContext).ConfigureAwait(false);
                await activity.UpdateAsync(taskContext, SH.ServiceBackgroundActivityDefaultDescription, false, false, false, false).ConfigureAwait(false);

                foreach (GitRepository info in infos)
                {
                    // Activity/catalogue awaits publish UI state. ConfigureAwait(false)
                    // alone does not prevent an inline continuation on that UI thread.
                    await taskContext.SwitchToBackgroundAsync();
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        try
                        {
                            ValueResult<bool, ValueDirectory> result = EnsureRepository(activity, directory, info, false, token);
                            succeeded = true;
                            return result;
                        }
                        catch (Exception first)
                        {
                            token.ThrowIfCancellationRequested();
                            exceptions.Add(first);
                            // A failed fetch is not evidence that cached data is corrupt.
                            if (IsUsableRepository(directory)) continue;
                            ValueResult<bool, ValueDirectory> result = EnsureRepository(activity, directory, info, true, token);
                            succeeded = true;
                            return result;
                        }
                    }
                    catch (Exception second)
                    {
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
                if (!failed && succeeded && !token.IsCancellationRequested && !XamlApplicationLifetime.Exiting)
                {
                    await activity.NotifyAsync(taskContext).ConfigureAwait(false);
                    await activity.UpdateAsync(taskContext, SH.ServiceGitRepositoryOperationCompleted, true, false, false, false).ConfigureAwait(false);
                }
            }

            token.ThrowIfCancellationRequested();
            await activity.NotifyAsync(taskContext).ConfigureAwait(false);
            await activity.UpdateAsync(taskContext, SH.ServiceGitRepositoryOperationFailed, false, true, false, false).ConfigureAwait(false);
            throw new GitRepositoryException(SH.ServiceGitRepositoryOperationFailed, exceptions);
        }
    }

    private ValueResult<bool, ValueDirectory> EnsureRepository(BackgroundActivity.BackgroundActivity activity, string directory, GitRepository info, bool forceInvalid, CancellationToken token)
    {
        long lastProgress = 0;
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
                if (token.IsCancellationRequested || XamlApplicationLifetime.Exiting) return false;
                long now = Stopwatch.GetTimestamp();
                if (now - lastProgress < Stopwatch.Frequency / 10) return true;
                lastProgress = now;
                int idx = output.AsSpan().IndexOfAny("\r\n");
                activity.Update(taskContext, idx > 0 ? output.Substring(0, idx) : output, false, false, false, false);
                return !token.IsCancellationRequested && !XamlApplicationLifetime.Exiting;
            },
            OnTransferProgress = progress =>
            {
                if (token.IsCancellationRequested || XamlApplicationLifetime.Exiting) return false;
                long now = Stopwatch.GetTimestamp();
                if (now - lastProgress < Stopwatch.Frequency / 10) return true;
                lastProgress = now;
                double progressValue = progress.TotalObjects == 0 ? 0 : (double)progress.ReceivedObjects / progress.TotalObjects;
                activity.Update(taskContext, $"{progress.ReceivedObjects}/{progress.TotalObjects}, {Converters.ToFileSizeString(progress.ReceivedBytes)}", false, false, true, false, progressValue);
                return !token.IsCancellationRequested && !XamlApplicationLifetime.Exiting;
            },
            CertificateCheck = static (cert, valid, host) => true,
        };

        if (forceInvalid || !IsUsableRepository(directory))
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

    private BackgroundActivity.BackgroundActivity GetActivityByName(string name)
    {
        return name switch
        {
            "Snap.Metadata" => backgroundActivityOptions.MetadataInitialization,
            "Snap.ContentDelivery" => backgroundActivityOptions.FullTrustInitialization,
            _ => backgroundActivityOptions.Default,
        };
    }

    private static bool IsUsableRepository(string directory)
    {
        if (!Repository.IsValid(directory)) return false;
        try
        {
            using Repository repository = new(directory);
            // A failed first clone can leave a valid .git directory with no HEAD.
            // Preserve usable cached data, not an unfinished clone that can never fetch.
            return repository.Head.Tip is not null;
        }
        catch (LibGit2SharpException) { return false; }
    }
}
