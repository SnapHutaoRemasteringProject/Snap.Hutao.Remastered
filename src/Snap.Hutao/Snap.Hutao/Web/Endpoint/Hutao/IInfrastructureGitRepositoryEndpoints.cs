// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Endpoint.Hutao;

internal interface IInfrastructureGitRepositoryEndpoints : IInfrastructureRootAccess
{
    string GitRepository(string name)
    {
        return $"{Root}/git-repository/all?name={name}";
    }
}