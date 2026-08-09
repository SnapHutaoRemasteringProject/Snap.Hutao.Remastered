// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;

namespace Snap.Hutao.Remastered.Service.Yae.PlayerStore;

public sealed class PlayerStoreResult
{
    public ByteString? StoreBytes { get; init; }

    public Dictionary<InterestedPropType, double> PropMap { get; init; } = [];
}
