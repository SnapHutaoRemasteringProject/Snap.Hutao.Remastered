// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;

[Service(ServiceLifetime.Transient, typeof(IOverseaSupportFactory<IUserClient>))]

internal sealed partial class UserClientFactory : OverseaSupportFactory<IUserClient, UserClient, UserClientOversea>
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial UserClientFactory(IServiceProvider serviceProvider);
}