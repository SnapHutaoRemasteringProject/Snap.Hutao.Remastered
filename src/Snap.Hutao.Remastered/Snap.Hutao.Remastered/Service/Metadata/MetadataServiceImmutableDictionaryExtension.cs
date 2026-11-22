// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Monster;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Metadata.Tower;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata;

public static class MetadataServiceImmutableDictionaryExtension
{
    extension(IMetadataService metadataService)
    {
        public ValueTask<ImmutableDictionary<EquipAffixId, ReliquarySet>> GetEquipAffixIdToReliquarySetMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<EquipAffixId, ReliquarySet>(
                MetadataCategories.ReliquarySet,
                r => r.EquipAffixId,
                token);
        }

        public ValueTask<ImmutableDictionary<ExtendedEquipAffixId, ReliquarySet>> GetExtendedEquipAffixIdToReliquarySetMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquarySet, ExtendedEquipAffixId, ReliquarySet>(
                MetadataCategories.ReliquarySet,
                array => array.SelectMany(set => set.EquipAffixIds, (set, id) => (Id: id, Set: set)),
                token);
        }

        public ValueTask<ImmutableDictionary<TowerLevelGroupId, ImmutableArray<TowerLevel>>> GetGroupIdToTowerLevelGroupMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<TowerLevel, IGrouping<TowerLevelGroupId, TowerLevel>, TowerLevelGroupId, ImmutableArray<TowerLevel>>(
                MetadataCategories.TowerLevel,
                array => array.GroupBy(l => l.GroupId),
                g => g.Key,
                ImmutableArray.ToImmutableArray,
                token);
        }

        public ValueTask<ImmutableDictionary<BydMaterialId, BydMaterial>> GetIdToBydMaterialMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<BydMaterialId, BydMaterial>(
                MetadataCategories.BydMaterial,
                token);
        }

        public ValueTask<ImmutableDictionary<AchievementId, Model.Metadata.Achievement.Achievement>> GetIdToAchievementMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<AchievementId, Model.Metadata.Achievement.Achievement>(MetadataCategories.Achievement, token);
        }

        public ValueTask<ImmutableDictionary<AvatarId, Avatar>> GetIdToAvatarMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<AvatarId, Avatar>(MetadataCategories.Avatar, token);
        }

        public ValueTask<ImmutableDictionary<PromoteId, ImmutableDictionary<PromoteLevel, Promote>>> GetIdToAvatarPromoteGroupMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<Promote, IGrouping<PromoteId, Promote>, PromoteId, ImmutableDictionary<PromoteLevel, Promote>>(
                MetadataCategories.AvatarPromote,
                array => array.GroupBy(p => p.Id),
                g => g.Key,
                g => g.ToImmutableDictionary(p => p.Level),
                token);
        }

        public async ValueTask<ImmutableDictionary<MaterialId, DisplayItem>> GetIdToDisplayItemAndMaterialMapAsync(CancellationToken token = default)
        {
            // Same key shape as every other projection: {prefix}.{category}.Map.{key}.{shape}.
            string cacheKey = metadataService.Cache.CreateKey(MetadataCategories.DisplayItem, "Map", nameof(MaterialId), "DisplayItemAndMaterial");
            return await metadataService.Cache.GetOrCreateAsync(cacheKey, async innerToken =>
            {
                ImmutableDictionary<MaterialId, DisplayItem> displays = await metadataService.FromCacheAsDictionaryAsync<MaterialId, DisplayItem>(MetadataCategories.DisplayItem, a => a.Id, innerToken).ConfigureAwait(false);
                ImmutableDictionary<MaterialId, Material> materials = await metadataService.GetIdToMaterialMapAsync(innerToken).ConfigureAwait(false);

                ImmutableDictionary<MaterialId, DisplayItem>.Builder results = ImmutableDictionary.CreateBuilder<MaterialId, DisplayItem>();
                results.AddRange(displays);

                foreach ((MaterialId id, DisplayItem material) in materials)
                {
                    results[id] = material;
                }

                return results.ToImmutable();
            }, token).ConfigureAwait(false);
        }

        public ValueTask<ImmutableDictionary<HardChallengeScheduleId, HardChallengeSchedule>> GetIdToHardChallengeScheduleMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<HardChallengeScheduleId, HardChallengeSchedule>(MetadataCategories.HardChallengeSchedule, token);
        }

        public ValueTask<ImmutableDictionary<HyperLinkNameId, HyperLinkName>> GetIdToHyperLinkNameMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<HyperLinkNameId, HyperLinkName>(MetadataCategories.HyperLinkName, token);
        }

        public ValueTask<ImmutableDictionary<MaterialId, Material>> GetIdToMaterialMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<MaterialId, Material>(MetadataCategories.Material, token);
        }

        public ValueTask<ImmutableDictionary<ReliquaryId, Reliquary>> GetIdToReliquaryMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<Reliquary, ReliquaryId, Reliquary>(
                MetadataCategories.Reliquary,
                array => array.SelectMany(r => r.Ids, (r, i) => (Index: i, Reliquary: r)),
                token);
        }

        public ValueTask<ImmutableDictionary<ReliquaryMainAffixId, FightProperty>> GetIdToReliquaryMainPropertyMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquaryMainAffix, ReliquaryMainAffixId, FightProperty>(
                MetadataCategories.ReliquaryMainAffix,
                r => r.Id,
                r => r.Type,
                token);
        }

        public ValueTask<ImmutableDictionary<ReliquarySetId, ReliquarySet>> GetIdToReliquarySetMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquarySetId, ReliquarySet>(
                MetadataCategories.ReliquarySet,
                r => r.SetId,
                token);
        }

        public ValueTask<ImmutableDictionary<ReliquarySubAffixId, ReliquarySubAffix>> GetIdToReliquarySubAffixMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquarySubAffixId, ReliquarySubAffix>(MetadataCategories.ReliquarySubAffix, token);
        }

        public ValueTask<ImmutableDictionary<RoleCombatScheduleId, RoleCombatSchedule>> GetIdToRoleCombatScheduleMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<RoleCombatScheduleId, RoleCombatSchedule>(MetadataCategories.RoleCombatSchedule, token);
        }

        public ValueTask<ImmutableDictionary<TowerFloorId, TowerFloor>> GetIdToTowerFloorMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<TowerFloorId, TowerFloor>(MetadataCategories.TowerFloor, token);
        }

        public ValueTask<ImmutableDictionary<TowerScheduleId, TowerSchedule>> GetIdToTowerScheduleMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<TowerScheduleId, TowerSchedule>(MetadataCategories.TowerSchedule, token);
        }

        public ValueTask<ImmutableDictionary<WeaponId, Weapon>> GetIdToWeaponMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<WeaponId, Weapon>(MetadataCategories.Weapon, token);
        }

        public ValueTask<ImmutableDictionary<PromoteId, ImmutableDictionary<PromoteLevel, Promote>>> GetIdToWeaponPromoteGroupMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<Promote, IGrouping<PromoteId, Promote>, PromoteId, ImmutableDictionary<PromoteLevel, Promote>>(
                MetadataCategories.WeaponPromote,
                array => array.GroupBy(p => p.Id),
                g => g.Key,
                g => g.ToImmutableDictionary(p => p.Level),
                token);
        }

        public ValueTask<ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>>> GetLevelToAvatarCurveMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<GrowCurve, Level, TypeValueCollection<GrowCurveType, float>>(
                MetadataCategories.AvatarCurve,
                a => a.Level,
                a => a.Curves,
                token);
        }

        public ValueTask<ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>>> GetLevelToMonsterCurveMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<GrowCurve, Level, TypeValueCollection<GrowCurveType, float>>(
                MetadataCategories.MonsterCurve,
                m => m.Level,
                m => m.Curves,
                token);
        }

        public ValueTask<ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>>> GetLevelToWeaponCurveMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<GrowCurve, Level, TypeValueCollection<GrowCurveType, float>>(
                MetadataCategories.WeaponCurve,
                w => w.Level,
                w => w.Curves,
                token);
        }

        public ValueTask<ImmutableDictionary<string, Avatar>> GetNameToAvatarMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<string, Avatar>(
                MetadataCategories.Avatar,
                a => a.Name,
                token);
        }

        public ValueTask<ImmutableDictionary<string, Weapon>> GetNameToWeaponMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<string, Weapon>(
                MetadataCategories.Weapon,
                w => w.Name,
                token);
        }

        public ValueTask<ImmutableDictionary<MonsterDescribeId, Monster>> GetDescribeIdToMonsterMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<MonsterDescribeId, Monster>(
                MetadataCategories.Monster,
                m => m.DescribeId,
                token);
        }

        public ValueTask<ImmutableDictionary<CookRecipeId, CookRecipe>> GetIdToCookRecipeMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<CookRecipeId, CookRecipe>(MetadataCategories.CookRecipe, token);
        }

        public ValueTask<ImmutableDictionary<MaterialId, Combine>> GetResultMaterialIdToCombineMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<MaterialId, Combine>(
                MetadataCategories.Combine,
                c => c.Result.Id,
                token);
        }

        private ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TKey, TValue>(MetadataCategory category, CancellationToken token)
            where TKey : notnull
            where TValue : class, IDefaultIdentity<TKey>
        {
            return FromCacheAsDictionaryAsync<TKey, TValue>(metadataService, category, v => v.Id, token);
        }

        private async ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TKey, TValue>(MetadataCategory category, Func<TValue, TKey> keySelector, CancellationToken token)
            where TKey : notnull
            where TValue : class
        {
            string keyName = TypeNameHelper.GetTypeDisplayName(typeof(TKey));
            string valueName = TypeNameHelper.GetTypeDisplayName(typeof(TValue));
            string cacheKey = metadataService.Cache.CreateKey(category, "Map", keyName, valueName);

            return await metadataService.Cache.GetOrCreateAsync(cacheKey, async innerToken =>
            {
                ImmutableArray<TValue> array = await metadataService.FromCacheOrSourceAsync<TValue>(category, innerToken).ConfigureAwait(false);
                return array.ToImmutableDictionaryIgnoringDuplicateKeys(keySelector); // There are duplicate name items
            }, token).ConfigureAwait(false);
        }

        private async ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TData, TKey, TValue>(MetadataCategory category, Func<TData, TKey> keySelector, Func<TData, TValue> valueSelector, CancellationToken token)
            where TKey : notnull
            where TData : class
        {
            string keyName = TypeNameHelper.GetTypeDisplayName(typeof(TKey));
            string valueName = TypeNameHelper.GetTypeDisplayName(typeof(TValue));
            string cacheKey = metadataService.Cache.CreateKey(category, "Map", keyName, valueName);

            return await metadataService.Cache.GetOrCreateAsync(cacheKey, async innerToken =>
            {
                ImmutableArray<TData> array = await metadataService.FromCacheOrSourceAsync<TData>(category, innerToken).ConfigureAwait(false);
                return array.ToImmutableDictionaryIgnoringDuplicateKeys(keySelector, valueSelector); // There are duplicate name items
            }, token).ConfigureAwait(false);
        }

        private ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TData, TKey, TValue>(MetadataCategory category, Func<ImmutableArray<TData>, IEnumerable<(TKey Key, TValue Value)>> transform, CancellationToken token)
            where TKey : notnull
            where TData : class
        {
            return FromCacheAsDictionaryAsync(metadataService, category, transform, kvp => kvp.Key, kvp => kvp.Value, token);
        }

        private async ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TData, TMiddle, TKey, TValue>(MetadataCategory category, Func<ImmutableArray<TData>, IEnumerable<TMiddle>> transform, Func<TMiddle, TKey> keySelector, Func<TMiddle, TValue> valueSelector, CancellationToken token)
            where TKey : notnull
            where TData : class
        {
            string keyName = TypeNameHelper.GetTypeDisplayName(typeof(TKey));
            string middleName = TypeNameHelper.GetTypeDisplayName(typeof(TMiddle));
            string valueName = TypeNameHelper.GetTypeDisplayName(typeof(TValue));
            string cacheKey = metadataService.Cache.CreateKey(category, "Map", keyName, middleName, valueName);

            return await metadataService.Cache.GetOrCreateAsync(cacheKey, async innerToken =>
            {
                ImmutableArray<TData> array = await metadataService.FromCacheOrSourceAsync<TData>(category, innerToken).ConfigureAwait(false);
                return transform(array).ToImmutableDictionaryIgnoringDuplicateKeys(keySelector, valueSelector); // There are duplicate name items
            }, token).ConfigureAwait(false);
        }
    }
}