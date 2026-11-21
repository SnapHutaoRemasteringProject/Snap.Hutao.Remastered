// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;

namespace Snap.Hutao.Remastered.Service.GachaLog.Factory;

public sealed class GachaStatisticsItemCounter
{
    public GachaStatisticsItemCounter(GachaStatisticsFactoryContext context)
    {
        if (!context.IsUnobtainedWishItemVisible)
        {
            OrangeAvatar = [];
            PurpleAvatar = [];
            OrangeWeapon = [];
            PurpleWeapon = [];
            BlueWeapon = [];
            OrangeBydMaterial = [];
            PurpleBydMaterial = [];
            BlueBydMaterial = [];
            return;
        }

        OrangeAvatar = context.Metadata.IdAvatarMap.Values
            .Where(avatar => avatar.Quality == QualityType.QUALITY_ORANGE)
            .ToDictionary(avatar => avatar, _ => 0);
        PurpleAvatar = context.Metadata.IdAvatarMap.Values
            .Where(avatar => avatar.Quality == QualityType.QUALITY_PURPLE)
            .ToDictionary(avatar => avatar, _ => 0);
        OrangeWeapon = context.Metadata.IdWeaponMap.Values
            .Where(weapon => weapon.Quality == QualityType.QUALITY_ORANGE)
            .ToDictionary(weapon => weapon, _ => 0);

        HashSet<Weapon> purpleWeapons = [];
        foreach (uint weaponId in WeaponIds.PurpleStandardWeaponIds)
        {
            purpleWeapons.Add(context.Metadata.GetWeapon(weaponId));
        }

        foreach (ref readonly GachaEvent gachaEvent in context.Metadata.GachaEvents.AsSpan())
        {
            if (gachaEvent.Type is GachaType.ActivityWeapon)
            {
                foreach (uint weaponId in gachaEvent.UpPurpleList)
                {
                    purpleWeapons.Add(context.Metadata.GetWeapon(weaponId));
                }
            }
        }

        HashSet<Weapon> blueWeapons = [];
        foreach (uint weaponId in WeaponIds.BlueStandardWeaponIds)
        {
            blueWeapons.Add(context.Metadata.GetWeapon(weaponId));
        }

        PurpleWeapon = purpleWeapons.ToDictionary(weapon => weapon, _ => 0);
        BlueWeapon = blueWeapons.ToDictionary(weapon => weapon, _ => 0);

        OrangeBydMaterial = context.Metadata.IdBydMaterialMap.Values
            .Where(bydMaterial => bydMaterial.Quality == QualityType.QUALITY_ORANGE)
            .ToDictionary(bydMaterial => bydMaterial, _ => 0);
        PurpleBydMaterial = context.Metadata.IdBydMaterialMap.Values
            .Where(bydMaterial => bydMaterial.Quality == QualityType.QUALITY_PURPLE)
            .ToDictionary(bydMaterial => bydMaterial, _ => 0);
        BlueBydMaterial = context.Metadata.IdBydMaterialMap.Values
            .Where(bydMaterial => bydMaterial.Quality == QualityType.QUALITY_BLUE)
            .ToDictionary(bydMaterial => bydMaterial, _ => 0);
    }

    public Dictionary<Avatar, int> OrangeAvatar { get; }

    public Dictionary<Avatar, int> PurpleAvatar { get; }

    public Dictionary<Weapon, int> OrangeWeapon { get; }

    public Dictionary<Weapon, int> PurpleWeapon { get; }

    public Dictionary<Weapon, int> BlueWeapon { get; }

    public Dictionary<BydMaterial, int> OrangeBydMaterial { get; }

    public Dictionary<BydMaterial, int> PurpleBydMaterial { get; }

    public Dictionary<BydMaterial, int> BlueBydMaterial { get; }
}