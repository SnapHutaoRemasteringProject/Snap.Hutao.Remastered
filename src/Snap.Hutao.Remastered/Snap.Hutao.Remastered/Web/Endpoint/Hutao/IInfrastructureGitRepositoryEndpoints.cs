// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

public interface IInfrastructureGitRepositoryEndpoints : IInfrastructureRootAccess
{
    string AllGitRepositories() => $"{Root}/git-repository/all";
    string GitRepository(string name)
    {
        return $"{Root}/git-repository/all?name={name}";
    }
}
