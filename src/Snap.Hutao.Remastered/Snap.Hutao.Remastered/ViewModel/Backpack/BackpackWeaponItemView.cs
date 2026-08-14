// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Service.Backpack;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public sealed class BackpackWeaponItemView : BackpackItemView
{
    public Weapon Weapon { get; private set; } = default!;

    public uint Level => Entity.Level;

    public uint RefinementRank => Entity.RefinementRank + 1;

    public bool IsLocked => Entity.IsLocked;

    public string WeaponTypeName => Weapon.WeaponType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!;

    public string LevelRefinementInfo => SH.FormatViewPageBackpackWeaponInfo(Level, RefinementRank);

    public static BackpackWeaponItemView Create(BackpackItem entity, BackpackServiceMetadataContext context, Weapon weapon)
    {
        return new()
        {
            Entity = entity,
            Category = BackpackItemCategory.Weapon,
            Weapon = weapon,
            Name = weapon.Name,
            Description = weapon.Description,
            TypeDescription = weapon.WeaponType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!,
            IconUri = EquipIconConverter.IconNameToUri(weapon.Icon),
            Quality = weapon.RankLevel,
        };
    }
}
