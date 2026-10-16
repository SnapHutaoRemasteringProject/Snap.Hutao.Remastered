// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Metadata.Tower;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.SpiralAbyss;

public sealed partial class MonsterView : ObservableObject, INameIcon<Uri>
{
    private MonsterView(in MonsterDescribeId id)
    {
        Name = $"Unknown {id}";
        Icon = StaticResourcesEndpoints.UIIconNone;
        Affixes = [];
        Count = 1;
    }

    private MonsterView(TowerMonster towerMonster, Model.Metadata.Monster.Monster metaMonster)
    {
        Name = metaMonster.Name ?? $"Unknown {towerMonster.Id}";
        Icon = MonsterIconConverter.IconNameToUri(metaMonster.Icon);
        Affixes = towerMonster.Affixes.EmptyIfDefault();
        Count = (int)towerMonster.Count;
        AttackMonolith = towerMonster.AttackMonolith;
    }

    public string Name { get; private set => SetProperty(ref field, value); }

    public Uri Icon { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<NameDescription> Affixes { get; private set => SetProperty(ref field, value); }

    public int Count { get; private set => SetProperty(ref field, value); }

    public bool IsCountOne { get => Count == 1; }

    public bool AttackMonolith { get; private set => SetProperty(ref field, value); }

    public static MonsterView Default(in MonsterDescribeId id)
    {
        return new(id);
    }

    public static MonsterView Create(TowerMonster tower, Model.Metadata.Monster.Monster meta)
    {
        return new(tower, meta);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(Count):
                OnPropertyChanged(nameof(IsCountOne));
                break;
        }
    }
}
