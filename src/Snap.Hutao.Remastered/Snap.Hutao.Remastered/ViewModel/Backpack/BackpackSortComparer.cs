// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control.AutoSortBox;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public static class BackpackSortComparer
{
    public static int CompareByKind(BackpackItemView x, BackpackItemView y, AutoSortTokenKind kind)
    {
        return kind switch
        {
            AutoSortTokenKind.Quality => GetQualityRank(x).CompareTo(GetQualityRank(y)),
            AutoSortTokenKind.Level => GetLevel(x).CompareTo(GetLevel(y)),
            AutoSortTokenKind.Refinement => GetRefinementRank(x).CompareTo(GetRefinementRank(y)),
            AutoSortTokenKind.WeaponType => GetWeaponType(x).CompareTo(GetWeaponType(y)),
            AutoSortTokenKind.EquipType => GetEquipType(x).CompareTo(GetEquipType(y)),
            AutoSortTokenKind.Score => GetScore(x).CompareTo(GetScore(y)),
            AutoSortTokenKind.SetName => string.CompareOrdinal(GetSetName(x), GetSetName(y)),
            AutoSortTokenKind.Name => string.CompareOrdinal(x.Name, y.Name),
            AutoSortTokenKind.Count => x.Entity.Count.CompareTo(y.Entity.Count),
            AutoSortTokenKind.Lock => GetLockState(x).CompareTo(GetLockState(y)),
            AutoSortTokenKind.Mark => GetMarkState(x).CompareTo(GetMarkState(y)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled sort token kind"),
        };
    }

    public static int GetQualityRank(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView w)
        {
            return (int)w.Weapon.RankLevel;
        }

        if (item is BackpackReliquaryItemView r)
        {
            return (int)r.Reliquary.RankLevel;
        }

        if (item.Material is not null)
        {
            return (int)item.Material.RankLevel;
        }

        return 0;
    }

    private static int GetLevel(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView wl)
        {
            return (int)wl.Level;
        }

        if (item is BackpackReliquaryItemView rl)
        {
            return (int)rl.Level;
        }

        return (int)item.Entity.Level;
    }

    private static int GetRefinementRank(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView wr)
        {
            return (int)wr.RefinementRank;
        }

        return 0;
    }

    private static int GetWeaponType(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView wt)
        {
            return (int)wt.Weapon.WeaponType;
        }

        return 0;
    }

    private static int GetEquipType(BackpackItemView item)
    {
        if (item is BackpackReliquaryItemView re)
        {
            return (int)re.Reliquary.EquipType;
        }

        return 0;
    }

    private static double GetScore(BackpackItemView item)
    {
        return item is BackpackReliquaryItemView rs ? rs.Score : 0D;
    }

    private static string GetSetName(BackpackItemView item)
    {
        return item is BackpackReliquaryItemView rsn ? (rsn.SetName ?? string.Empty) : string.Empty;
    }

    private static int GetLockState(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView wlk)
        {
            return wlk.IsLocked ? 1 : 0;
        }

        if (item is BackpackReliquaryItemView rlk)
        {
            return rlk.IsLocked ? 1 : 0;
        }

        return 0;
    }

    private static int GetMarkState(BackpackItemView item)
    {
        return item is BackpackReliquaryItemView rm ? (rm.IsMarked ? 1 : 0) : 0;
    }
}
