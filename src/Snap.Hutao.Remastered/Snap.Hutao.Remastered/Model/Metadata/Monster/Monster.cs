// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Monster;

public sealed partial class Monster : ObservableObject, IPropertyValuesProvider
{
    public const uint MaxLevel = 110U;

    public required MonsterId Id { get; init => SetProperty(ref field, value); }

    public required MonsterDescribeId DescribeId { get; init => SetProperty(ref field, value); }

    public string? MonsterName { get; init => SetProperty(ref field, value); }

    public string? Name { get; init => SetProperty(ref field, value); }

    public string? Title { get; init => SetProperty(ref field, value); }

    public string? Description { get; init => SetProperty(ref field, value); }

    public required string Icon { get; init => SetProperty(ref field, value); }

    public required MonsterType Type { get; init => SetProperty(ref field, value); }

    public required Arkhe Arkhe { get; init => SetProperty(ref field, value); }

    public List<string>? Affixes { get; init => SetProperty(ref field, value); }

    public ImmutableArray<MaterialId> Drops { get; init => SetProperty(ref field, value); }

    public MonsterBaseValue? BaseValue { get; init => SetProperty(ref field, value); }

    public TypeValueCollection<FightProperty, GrowCurveType>? GrowCurves { get; init => SetProperty(ref field, value); }

    [JsonIgnore]
    [ObservableProperty]
    public partial ImmutableArray<DisplayItem>? DropsView { get; set; }
}
