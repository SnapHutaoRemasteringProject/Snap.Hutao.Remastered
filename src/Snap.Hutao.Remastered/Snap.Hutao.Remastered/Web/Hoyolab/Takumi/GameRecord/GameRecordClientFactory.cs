// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;

[Service(ServiceLifetime.Transient, typeof(IOverseaSupportFactory<IGameRecordClient>))]
internal sealed partial class GameRecordClientFactory : OverseaSupportFactory<IGameRecordClient, GameRecordClient, GameRecordClientOversea>
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial GameRecordClientFactory(IServiceProvider serviceProvider);
}