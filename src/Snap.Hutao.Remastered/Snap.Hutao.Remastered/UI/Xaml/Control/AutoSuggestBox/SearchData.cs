// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Monster;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;

public sealed partial class SearchData : ObservableObject
{
    private SearchData(FrozenDictionary<string, SearchToken> availableTokens)
    {
        AvailableTokens = availableTokens;
    }

    public FrozenDictionary<string, SearchToken> AvailableTokens { get; }

    public ObservableCollection<SearchToken> FilterTokens { get; } = [];

    [ObservableProperty]
    public partial string? FilterToken { get; set; }

    public static SearchData CreateForAvatarProperty()
    {
        return new(SearchTokens.GetForAvatarProperty());
    }

    public static SearchData CreateForCultivation()
    {
        return new(SearchTokens.GetForCultivation());
    }

    public static SearchData CreateForWikiAvatar(ImmutableArray<Avatar> array)
    {
        return new(SearchTokens.GetForWikiAvatar(array));
    }

    public static SearchData CreateForWikiWeapon(ImmutableArray<Weapon> array)
    {
        return new(SearchTokens.GetForWikiWeapon(array));
    }

    public static SearchData CreateForWikiMonster(ImmutableArray<Monster> array)
    {
        return new(SearchTokens.GetForWikiMonster(array));
    }

    public static SearchData Create(FrozenDictionary<string, SearchToken> availableTokens)
    {
        return new(availableTokens);
    }
}