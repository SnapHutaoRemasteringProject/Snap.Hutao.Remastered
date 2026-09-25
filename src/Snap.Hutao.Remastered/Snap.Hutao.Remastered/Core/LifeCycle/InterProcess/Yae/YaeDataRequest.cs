// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;

public sealed class YaeDataRequest
{
    public static ImmutableArray<InterestedPropType> AllPlayerPropTypes { get; } = [.. Enum.GetValues<InterestedPropType>().Where(static type => type is not InterestedPropType.None)];

    public required ImmutableArray<uint> PacketCmdIds { get; init; }

    public required ImmutableArray<InterestedPropType> PlayerPropTypes { get; init; }
}
