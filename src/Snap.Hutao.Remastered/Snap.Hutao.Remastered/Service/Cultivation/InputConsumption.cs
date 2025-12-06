// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity.Primitive;
using Snap.Hutao.Remastered.Service.Cultivation.Consumption;
using System.Collections.Immutable;
using CalculateItem = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.Calculate.Item;

namespace Snap.Hutao.Remastered.Service.Cultivation;

internal sealed class InputConsumption
{
    public required CultivateType Type { get; init; }

    public required uint ItemId { get; init; }

    public required ImmutableArray<CalculateItem> Items { get; init; }

    public required LevelInformation LevelInformation { get; init; }

    public required ConsumptionSaveStrategyKind Strategy { get; init; }
}