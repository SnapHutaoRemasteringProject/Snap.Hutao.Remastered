// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LibGit2Sharp;
using Snap.Hutao.Core;
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
                config.Unset("http.proxy");
                config.Unset("https.proxy");

                Remote remote = repo.Network.Remotes.Add("origin", sourceUrl);
                Commands.Fetch(repo, remote.Name, WinRTAdaptive.Array(["+refs/heads/main:refs/remotes/origin/main"]), options.FetchOptions, default);
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