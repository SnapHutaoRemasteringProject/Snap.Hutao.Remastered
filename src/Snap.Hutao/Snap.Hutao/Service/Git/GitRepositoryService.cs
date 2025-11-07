// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LibGit2Sharp;
using Snap.Hutao.Core;
using Snap.Hutao.Core.IO;
using Snap.Hutao.Core.IO.Http.Proxy;
using Snap.Hutao.Web.Hutao;
using Snap.Hutao.Web.Hutao.Response;
using Snap.Hutao.Web.Response;
using System.Collections.Immutable;
using System.IO;

namespace Snap.Hutao.Service.Git;

[Service(ServiceLifetime.Singleton, typeof(IGitRepositoryService))]
internal sealed partial class GitRepositoryService : IGitRepositoryService
{
    private readonly IServiceProvider serviceProvider;

    [GeneratedConstructor]
    public partial GitRepositoryService(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, ValueDirectory>> EnsureRepositoryAsync(string name)
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

        GitRepository info = infos.Single();

        FetchOptions fetchOptions = new()
        {
            Depth = 1,
            ProxyOptions =
            {
                ProxyType = HttpProxyUsingSystemProxy.Instance.CurrentProxyUri is null ? ProxyType.None : ProxyType.Specified,
                Url = HttpProxyUsingSystemProxy.Instance.CurrentProxyUri,
            },
            CredentialsProvider = (url, user, types) => string.IsNullOrEmpty(info.Token)
                ? default
                : new UsernamePasswordCredentials
                {
                    Username = "TODO",
                    Password = info.Token,
                },
        };

        if (!Repository.IsValid(directory))
        {
            if (Directory.Exists(directory))
            {
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

                Commands.Fetch(repo, "origin", WinRTAdaptive.Array(["+refs/heads/main:refs/remotes/origin/main"]), fetchOptions, default);
                Branch remoteBranch = repo.Branches["origin/main"];
                Branch localBranch = repo.Branches["main"];
                if (localBranch is null)
                {
                    localBranch = repo.CreateBranch("main", remoteBranch.Tip);
                    repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
                }

                Commands.Checkout(repo, localBranch, new()
                {
                    CheckoutModifiers = CheckoutModifiers.Force,
                });

                repo.Reset(ResetMode.Hard, remoteBranch.Tip);
            }
        }

        return new(true, directory);
    }
}