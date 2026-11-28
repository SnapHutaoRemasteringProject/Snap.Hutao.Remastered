// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Monster;

[ProtoContract]
public sealed partial class Monster : ObservableObject, IPropertyValuesProvider
{
    public const uint MaxLevel = 110U;

    [ProtoMember(1)]
    public MonsterId Id { get; init => SetProperty(ref field, value); }

    [ProtoMember(2)]
    public MonsterDescribeId DescribeId { get; init => SetProperty(ref field, value); }

    [ProtoMember(3)]
    public string? MonsterName { get; init => SetProperty(ref field, value); }

    [ProtoMember(4)]
    public string? Name { get; init => SetProperty(ref field, value); }

    [ProtoMember(5)]
    public string? Title { get; init => SetProperty(ref field, value); }

    [ProtoMember(6)]
    public string? Description { get; init => SetProperty(ref field, value); }

    [ProtoMember(7)]
    public string Icon { get; init => SetProperty(ref field, value); }

    [ProtoMember(8)]
    public MonsterType Type { get; init => SetProperty(ref field, value); }

    [ProtoMember(9)]
    public Arkhe Arkhe { get; init => SetProperty(ref field, value); }

    [ProtoMember(10)]
    public List<string>? Affixes { get; init => SetProperty(ref field, value); }

    [ProtoMember(11)]
    public ImmutableArray<MaterialId> Drops { get; init => SetProperty(ref field, value); }

    [ProtoMember(12)]
    public MonsterBaseValue? BaseValue { get; init => SetProperty(ref field, value); }

    [ProtoMember(13)]
    public TypeValueCollection<FightProperty, GrowCurveType>? GrowCurves { get; init => SetProperty(ref field, value); }

    [JsonIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DropsItemSource))]
    public partial ImmutableArray<DisplayItem>? DropsView { get; set; }

    [JsonIgnore]
    public List<DisplayItem>? DropsItemSource => DropsView is { } items ? items.IsDefaultOrEmpty ? [] : new(items) : null;
}
