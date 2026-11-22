// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Achievement;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Monster;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Metadata.Tower;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata;

public static class MetadataServiceImmutableArrayExtension
{
    extension(IMetadataService metadataService)
    {
        public ValueTask<ImmutableArray<Model.Metadata.Achievement.Achievement>> GetAchievementArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Model.Metadata.Achievement.Achievement>(MetadataCategories.Achievement, token);
        }

        public ValueTask<ImmutableArray<Chapter>> GetChapterArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Chapter>(MetadataCategories.Chapter, token);
        }

        public ValueTask<ImmutableArray<CookRecipe>> GetCookRecipeArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<CookRecipe>(MetadataCategories.CookRecipe, token);
        }

        public ValueTask<ImmutableArray<AchievementGoal>> GetAchievementGoalArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<AchievementGoal>(MetadataCategories.AchievementGoal, token);
        }

        public ValueTask<ImmutableArray<Avatar>> GetAvatarArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Avatar>(MetadataCategories.Avatar, token);
        }

        public ValueTask<ImmutableArray<GrowCurve>> GetAvatarCurveArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<GrowCurve>(MetadataCategories.AvatarCurve, token);
        }

        public ValueTask<ImmutableArray<Promote>> GetAvatarPromoteArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Promote>(MetadataCategories.AvatarPromote, token);
        }

        public ValueTask<ImmutableArray<DisplayItem>> GetDisplayItemArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<DisplayItem>(MetadataCategories.DisplayItem, token);
        }

        public ValueTask<ImmutableArray<GachaEvent>> GetGachaEventArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<GachaEvent>(MetadataCategories.GachaEvent, token);
        }

        public ValueTask<ImmutableArray<HyperLinkName>> GetHyperLinkNameArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<HyperLinkName>(MetadataCategories.HyperLinkName, token);
        }

        public ValueTask<ImmutableArray<MainQuest>> GetMainQuestArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<MainQuest>(MetadataCategories.MainQuest, token);
        }

        public ValueTask<ImmutableArray<Material>> GetMaterialArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Material>(MetadataCategories.Material, token);
        }

        public ValueTask<ImmutableArray<Monster>> GetMonsterArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Monster>(MetadataCategories.Monster, token);
        }

        public ValueTask<ImmutableArray<GrowCurve>> GetMonsterCurveArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<GrowCurve>(MetadataCategories.MonsterCurve, token);
        }

        public ValueTask<ImmutableArray<ProfilePicture>> GetProfilePictureArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<ProfilePicture>(MetadataCategories.ProfilePicture, token);
        }

        public ValueTask<ImmutableArray<Reliquary>> GetReliquaryArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Reliquary>(MetadataCategories.Reliquary, token);
        }

        public ValueTask<ImmutableArray<ReliquaryMainAffix>> GetReliquaryMainAffixArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<ReliquaryMainAffix>(MetadataCategories.ReliquaryMainAffix, token);
        }

        public ValueTask<ImmutableArray<ReliquaryMainAffixLevel>> GetReliquaryMainAffixLevelArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<ReliquaryMainAffixLevel>(MetadataCategories.ReliquaryMainAffixLevel, token);
        }

        public ValueTask<ImmutableArray<ReliquarySet>> GetReliquarySetArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<ReliquarySet>(MetadataCategories.ReliquarySet, token);
        }

        public ValueTask<ImmutableArray<ReliquarySubAffix>> GetReliquarySubAffixArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<ReliquarySubAffix>(MetadataCategories.ReliquarySubAffix, token);
        }

        public ValueTask<ImmutableArray<TowerFloor>> GetTowerFloorArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<TowerFloor>(MetadataCategories.TowerFloor, token);
        }

        public ValueTask<ImmutableArray<TowerLevel>> GetTowerLevelArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<TowerLevel>(MetadataCategories.TowerLevel, token);
        }

        public ValueTask<ImmutableArray<TowerSchedule>> GetTowerScheduleArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<TowerSchedule>(MetadataCategories.TowerSchedule, token);
        }

        public ValueTask<ImmutableArray<Weapon>> GetWeaponArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Weapon>(MetadataCategories.Weapon, token);
        }

        public ValueTask<ImmutableArray<GrowCurve>> GetWeaponCurveArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<GrowCurve>(MetadataCategories.WeaponCurve, token);
        }

        public ValueTask<ImmutableArray<Promote>> GetWeaponPromoteArrayAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheOrSourceAsync<Promote>(MetadataCategories.WeaponPromote, token);
        }
    }
}