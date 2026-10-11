// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

[Service(ServiceLifetime.Singleton, typeof(IHutaoEndpoints), Key = HutaoEndpointsKind.AlphaOS)]
public sealed class HutaoEndpointsForAlphaOS : IHutaoEndpoints
{
    private readonly IServerDomainService serverDomain;

    public HutaoEndpointsForAlphaOS(IServerDomainService serverDomain)
    {
        this.serverDomain = serverDomain;
    }

    string IHomaRootAccess.Root { get => serverDomain.GetHomaRoot(); }

    string IInfrastructureRootAccess.Root { get => "https://alpha.snapgenshin.cn/global"; }

    string IInfrastructureRawRootAccess.RawRoot { get => "https://alpha.snapgenshin.cn"; }

    public string PatchSnapHutao()
    {
        return $"{((IInfrastructureRootAccess)this).Root}/patch/alpha";
    }
}