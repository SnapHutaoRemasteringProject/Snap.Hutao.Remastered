// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Passport;

[Service(ServiceLifetime.Transient, typeof(IOverseaSupportFactory<IHoyoPlayPassportClient>))]
internal sealed partial class HoyoPlayPassportClientFactory : OverseaSupportFactory<IHoyoPlayPassportClient, HoyoPlayPassportClient, HoyoPlayPassportClientOversea>
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial HoyoPlayPassportClientFactory(IServiceProvider serviceProvider);
}