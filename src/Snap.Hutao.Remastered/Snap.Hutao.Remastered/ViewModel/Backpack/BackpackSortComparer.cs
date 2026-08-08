// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.UI.Xaml.Control.AutoSortBox;
using System.Runtime.CompilerServices;

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

    private static int GetQualityRank(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView w)
        {
            QualityType rank = w.Weapon.RankLevel;
            return Unsafe.As<QualityType, int>(ref rank);
        }

        if (item is BackpackReliquaryItemView r)
        {
            QualityType rank = r.Reliquary.RankLevel;
            return Unsafe.As<QualityType, int>(ref rank);
        }

        if (item.Material is not null)
        {
            QualityType rank = item.Material.RankLevel;
            return Unsafe.As<QualityType, int>(ref rank);
        }

        return 0;
    }

    private static int GetLevel(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView wl)
        {
            uint level = wl.Level;
            return Unsafe.As<uint, int>(ref level);
        }

        if (item is BackpackReliquaryItemView rl)
        {
            uint level = rl.Level;
            return Unsafe.As<uint, int>(ref level);
        }

        uint rootLevel = item.Entity.Level;
        return Unsafe.As<uint, int>(ref rootLevel);
    }

    private static int GetRefinementRank(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView wr)
        {
            uint rank = wr.RefinementRank;
            return Unsafe.As<uint, int>(ref rank);
        }

        return 0;
    }

    private static int GetWeaponType(BackpackItemView item)
    {
        if (item is BackpackWeaponItemView wt)
        {
            WeaponType type = wt.Weapon.WeaponType;
            return Unsafe.As<WeaponType, int>(ref type);
        }

        return 0;
    }

    private static int GetEquipType(BackpackItemView item)
    {
        if (item is BackpackReliquaryItemView re)
        {
            EquipType type = re.Reliquary.EquipType;
            return Unsafe.As<EquipType, int>(ref type);
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
