// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class CookRecipe : IDefaultIdentity<CookRecipeId>
{
    [ProtoMember(1)]
    public CookRecipeId Id { get; init; }

    [ProtoMember(2)]
    public string Name { get; init; }

    [ProtoMember(3)]
    public string Description { get; init; }

    [ProtoMember(4)]
    public ImmutableArray<string> EffectDescription { get; init; } = [];

    [ProtoMember(5)]
    public CookMethod CookMethod { get; init; }

    [ProtoMember(6)]
    public CookFoodType FoodType { get; init; }

    [ProtoMember(7)]
    public string Icon { get; init; }

    [ProtoMember(8)]
    public bool IsDefaultUnlocked { get; init; }

    [ProtoMember(9)]
    public uint MaxProficiency { get; init; }

    [ProtoMember(10)]
    public QualityType RankLevel { get; init; }

    [ProtoMember(11)]
    public ImmutableArray<IdCount> QualityOutput { get; init; } = [];

    [ProtoMember(12)]
    public ImmutableArray<IdCount> Input { get; init; } = [];
}
