// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Web.Hutao.SpiralAbyss;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Web.Hutao.RoleCombat;

internal sealed class RoleCombatStatisticsItem
{
    public int RecordTotal { get; init; }

    public ImmutableArray<ItemRate<AvatarId, double>> BackupAvatarRates { get; init; }
}