// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Endpoint.Hutao;

internal interface IInfrastructureEndpoints :
    IInfrastructureEnkaEndpoints,
    IInfrastructureStrategyEndpoints,
    IInfrastructureFeatureEndpoints,
    IInfrastructureMetadataEndpoints,
    IInfrastructurePatchEndpoints,
    IInfrastructureGitRepositoryEndpoints,
    IInfrastructureWallpaperEndpoints,
    IInfrastructureRootAccess,
    IInfrastructureManagementEndpoints
{
    string Ip()
    {
        return $"{Root}/ip";
    }

    string IpString()
    {
        return $"{Root}/ips";
    }
}