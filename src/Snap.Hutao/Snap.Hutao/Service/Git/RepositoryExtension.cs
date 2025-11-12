// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LibGit2Sharp;
using System.IO;

namespace Snap.Hutao.Service.Git;

internal static class RepositoryExtension
{
    extension(Repository)
    {
        public static void AdvancedClone(string sourceUrl, string workdirPath, CloneOptions options)
        {
            Directory.CreateDirectory(workdirPath);
            Repository.Init(workdirPath);
            using (Repository repo = new(workdirPath))
            {
                Configuration config = repo.Config;
                config.Set("core.longpaths", true);
                config.Set("safe.directory", true);
                if (string.IsNullOrEmpty(options.FetchOptions.ProxyOptions.Url))
                {
                    config.Unset("http.proxy");
                    config.Unset("https.proxy");
                }
                else
                {
                    config.Set("http.proxy", options.FetchOptions.ProxyOptions.Url);
                    config.Set("https.proxy", options.FetchOptions.ProxyOptions.Url);
                }

                Remote remote = repo.Network.Remotes.Add("origin", sourceUrl);
                options.FetchOptions.UpdateFetchHead = false;
                Commands.Fetch(repo, remote.Name, Array.Empty<string>(), options.FetchOptions, default);
                Branch remoteBranch = repo.Branches["origin/main"];
                Branch localBranch = repo.CreateBranch("main", remoteBranch.Tip);
                repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);

                if (options.Checkout)
                {
                    Commands.Checkout(repo, localBranch);
                }
            }
        }
    }
}