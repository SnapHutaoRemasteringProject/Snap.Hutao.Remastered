// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using LibGit2Sharp;
using Snap.Hutao.Core;
using Snap.Hutao.Core.IO;
using Snap.Hutao.Core.IO.Http.Proxy;
using Snap.Hutao.Web.Hutao;
using Snap.Hutao.Web.Hutao.Response;
using Snap.Hutao.Web.Response;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Snap.Hutao.Service.Git;

[Service(ServiceLifetime.Singleton, typeof(IGitRepositoryService))]
internal sealed partial class GitRepositoryService : IGitRepositoryService
{
    private readonly AsyncKeyedLock<string> repoLock = new();
    private readonly IServiceProvider serviceProvider;
    private readonly IMessenger messenger;

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

            string directory = Path.GetFullPath(Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), name));

            List<Exception> exceptions = [];
            foreach (GitRepository info in RepositoryAffinity.Sort(infos))
            {
                try
                {
                    try
                    {
                        return EnsureRepository(directory, info, false);
                    }
                    catch (Exception first)
                    {
                        exceptions.Add(first);
                        RepositoryAffinity.IncreaseFailure(info.Name, info.HttpsUrl.OriginalString);
                        return EnsureRepository(directory, info, true);
                    }
                }
                catch (Exception second)
                {
                    RepositoryAffinity.IncreaseFailure(info.Name, info.HttpsUrl.OriginalString);
                    exceptions.Add(second);
                }
            }

            throw new GitRepositoryException("All repository sources failed.", exceptions);
        }
    }

    private static ValueResult<bool, ValueDirectory> EnsureRepository(string directory, GitRepository info, bool forceInvalid)
    {
        FetchOptions fetchOptions = new()
        {
            Depth = 1,
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
            OnProgress = static output =>
            {
                Debug.Write($"[Repo] {output}");
                return true;
            },
            OnTransferProgress = static progress =>
            {
                Debug.WriteLine($"[Repo Progress] {progress.ReceivedObjects}/{progress.TotalObjects}, {Converters.ToFileSizeString(progress.ReceivedBytes)}");
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
                fetchOptions.Depth = 0;
                Commands.Fetch(repo, repo.Head.RemoteName, Array.Empty<string>(), fetchOptions, default);

                Branch remoteBranch = repo.Branches["origin/main"];
                Branch localBranch = repo.Branches["main"] ?? repo.CreateBranch("main", remoteBranch.Tip);
                repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
                Commands.Checkout(repo, localBranch);
            }
        }

        return new(true, directory);
    }
}