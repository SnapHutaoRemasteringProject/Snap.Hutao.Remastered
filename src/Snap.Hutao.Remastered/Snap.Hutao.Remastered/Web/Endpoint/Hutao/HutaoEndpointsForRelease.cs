// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

[Service(ServiceLifetime.Singleton, typeof(IHutaoEndpoints), Key = HutaoEndpointsKind.Release)]
internal sealed class HutaoEndpointsForRelease : IHutaoEndpoints
{
    string IHomaRootAccess.Root { get => "https://htserver.wdg.cloudns.ch/api"; }

    string IInfrastructureRootAccess.Root { get => "https://htserver.wdg.cloudns.ch/api"; }

    string IInfrastructureRawRootAccess.RawRoot { get => "https://htserver.wdg.cloudns.ch/api"; }
}