// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

[Service(ServiceLifetime.Singleton, typeof(IHutaoEndpoints), Key = HutaoEndpointsKind.AlphaCN)]
public sealed class HutaoEndpointsForAlphaCN : IHutaoEndpoints
{
    private readonly IServerDomainService serverDomain;

    public HutaoEndpointsForAlphaCN(IServerDomainService serverDomain)
    {
        this.serverDomain = serverDomain;
    }

    string IHomaRootAccess.Root { get => serverDomain.GetHomaRoot(); }

    string IInfrastructureRootAccess.Root { get => "https://alpha.snapgenshin.cn/cn"; }

    string IInfrastructureRawRootAccess.RawRoot { get => "https://alpha.snapgenshin.cn"; }

    public string PatchSnapHutao()
    {
        return $"{((IInfrastructureRootAccess)this).Root}/patch/alpha";
    }
}