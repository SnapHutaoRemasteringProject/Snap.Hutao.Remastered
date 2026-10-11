// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

[Service(ServiceLifetime.Singleton, typeof(IHutaoEndpoints), Key = HutaoEndpointsKind.Release)]
public sealed class HutaoEndpointsForRelease : IHutaoEndpoints
{
    private readonly IServerDomainService serverDomain;

    public HutaoEndpointsForRelease(IServerDomainService serverDomain)
    {
        this.serverDomain = serverDomain;
    }

    string IHomaRootAccess.Root { get => serverDomain.GetHomaRoot(); }

    string IInfrastructureRootAccess.Root { get => serverDomain.GetApiRoot(); }

    string IInfrastructureRawRootAccess.RawRoot { get => serverDomain.GetApiRoot(); }
}
