// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Calculable;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Primitive;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Collections.Immutable;
using System.Text;

namespace Snap.Hutao.Remastered.ViewModel.Cultivation;

public sealed partial class CultivateEntryView : Item, IPropertyValuesProvider
{
    private CultivateEntryView(CultivateEntry entry, Item item, ImmutableArray<CultivateItemView> items)
    {
        Entry = entry;
        Id = entry.Id;
        EntryId = entry.InnerId;
        Name = item.Name;
        Icon = item.Icon;
        Badge = item.Badge;
        Quality = item.Quality;
        Items = items;
        Type = entry.Type;

        Description = ParseDescription(entry);
        IsToday = items.Any(i => i.IsToday);
        RotationalItemIds = [.. items.Where(i => i.DaysOfWeek is not DaysOfWeek.Any).Select(i => i.Inner.Id)];
        DaysOfWeek = MaterialIds.GetDaysOfWeek(RotationalItemIds.AsSpan());

        static string ParseDescription(CultivateEntry entry)
        {
            if (entry.LevelInformation is null)
            {
                return SH.ViewModelCultivationEntryViewDescriptionDefault;
            }

            CultivateEntryLevelInformation info = entry.LevelInformation;

            switch (entry.Type)
            {
                case CultivateType.AvatarAndSkill:
                    {
                        StringBuilder stringBuilder = new();

                        if (info.AvatarLevelFrom != info.AvatarLevelTo)
                        {
                            stringBuilder.Append("Lv.").Append(info.AvatarLevelFrom).Append(" → Lv.").Append(info.AvatarLevelTo).Append(' ');
                            stringBuilder.AppendLine();
                        }
                        else
                        {
                            if (info.AvatarIsPromoting)
                            {
                                stringBuilder.Append("Lv.").Append(info.AvatarLevelFrom).Append(" (").Append(SH.ViewModelCultivationEntryViewPromoteOnlyHint).Append(')');
                            }
                        }

                        if (info.SkillALevelFrom != info.SkillALevelTo)
                        {
                            stringBuilder.Append("A: ").Append(info.SkillALevelFrom).Append(" → ").Append(info.SkillALevelTo).Append(' ');
                        }

                        if (info.SkillELevelFrom != info.SkillELevelTo)
                        {
                            stringBuilder.Append("E: ").Append(info.SkillELevelFrom).Append(" → ").Append(info.SkillELevelTo).Append(' ');
                        }

                        if (info.SkillQLevelFrom != info.SkillQLevelTo)
                        {
                            stringBuilder.Append("Q: ").Append(info.SkillQLevelFrom).Append(" → ").Append(info.SkillQLevelTo).Append(' ');
                        }

                        return stringBuilder.ToStringTrimEndNewLine();
                    }

                case CultivateType.Weapon:
                    {
                        StringBuilder stringBuilder = new();

                        if (info.WeaponLevelFrom != info.WeaponLevelTo)
                        {
                            stringBuilder.Append("Lv.").Append(info.WeaponLevelFrom).Append(" → Lv.").Append(info.WeaponLevelTo);
                        }
                        else
                        {
                            if (info.WeaponIsPromoting)
                            {
                                stringBuilder.Append("Lv.").Append(info.WeaponLevelFrom).Append(" (").Append(SH.ViewModelCultivationEntryViewPromoteOnlyHint).Append(')');
                            }
                        }

                        return stringBuilder.ToString();
                    }
            }

            return string.Empty;
        }
    }

    public CultivateEntry Entry { get; }

    public CalculableAvatar? Avatar { get; private set; }

    public CalculableWeapon? Weapon { get; private set; }

    public ImmutableArray<CultivateItemView> Items { get; set; }

    public ImmutableArray<MaterialId> RotationalItemIds { get; }

    public bool IsToday { get; }

    public DaysOfWeek DaysOfWeek { get; }

    public string Description { get; }

    public Guid EntryId { get; }

    public CultivateType Type { get; }

    public static CultivateEntryView Create(CultivateEntry entry, Item item, ImmutableArray<CultivateItemView> items, CalculableAvatar? calculableAvatar = null, CalculableWeapon? calculableWeapon = null)
    {
        CultivateEntryView view = new(entry, item, items);
        view.Avatar = calculableAvatar;
        view.Weapon = calculableWeapon;

        if (calculableAvatar is not null && entry.LevelInformation is { } levelInfo)
        {
            calculableAvatar.LevelCurrent = levelInfo.AvatarLevelFrom;
            calculableAvatar.LevelTarget = levelInfo.AvatarLevelTo;
            calculableAvatar.IsPromoted = levelInfo.AvatarIsPromoting;

            if (calculableAvatar.Skills is [{ } skillA, { } skillE, { } skillQ, ..])
            {
                skillA.LevelCurrent = levelInfo.SkillALevelFrom;
                skillA.LevelTarget = levelInfo.SkillALevelTo;
                skillE.LevelCurrent = levelInfo.SkillELevelFrom;
                skillE.LevelTarget = levelInfo.SkillELevelTo;
                skillQ.LevelCurrent = levelInfo.SkillQLevelFrom;
                skillQ.LevelTarget = levelInfo.SkillQLevelTo;
            }
        }

        if (calculableWeapon is not null && entry.LevelInformation is { } weaponLevelInfo)
        {
            calculableWeapon.LevelCurrent = weaponLevelInfo.WeaponLevelFrom;
            calculableWeapon.LevelTarget = weaponLevelInfo.WeaponLevelTo;
            calculableWeapon.IsPromoted = weaponLevelInfo.WeaponIsPromoting;
        }

        return view;
    }
}