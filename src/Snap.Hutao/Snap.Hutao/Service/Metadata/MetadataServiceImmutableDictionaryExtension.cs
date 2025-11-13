// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Snap.Hutao.Core;
using Snap.Hutao.Model;
using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Model.Metadata;
using Snap.Hutao.Model.Metadata.Avatar;
using Snap.Hutao.Model.Metadata.Item;
using Snap.Hutao.Model.Metadata.Monster;
using Snap.Hutao.Model.Metadata.Reliquary;
using Snap.Hutao.Model.Metadata.Tower;
using Snap.Hutao.Model.Metadata.Weapon;
using Snap.Hutao.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Service.Metadata;

internal static class MetadataServiceImmutableDictionaryExtension
{
    extension(IMetadataService metadataService)
    {
        public ValueTask<ImmutableDictionary<EquipAffixId, ReliquarySet>> GetEquipAffixIdToReliquarySetMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<EquipAffixId, ReliquarySet>(
                MetadataFileStrategies.ReliquarySet,
                r => r.EquipAffixId,
                token);
        }

        public ValueTask<ImmutableDictionary<ExtendedEquipAffixId, ReliquarySet>> GetExtendedEquipAffixIdToReliquarySetMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquarySet, ExtendedEquipAffixId, ReliquarySet>(
                MetadataFileStrategies.ReliquarySet,
                array => array.SelectMany(set => set.EquipAffixIds, (set, id) => (Id: id, Set: set)),
                token);
        }

        public ValueTask<ImmutableDictionary<TowerLevelGroupId, ImmutableArray<TowerLevel>>> GetGroupIdToTowerLevelGroupMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<TowerLevel, IGrouping<TowerLevelGroupId, TowerLevel>, TowerLevelGroupId, ImmutableArray<TowerLevel>>(
                MetadataFileStrategies.TowerLevel,
                array => array.GroupBy(l => l.GroupId),
                g => g.Key,
                ImmutableArray.ToImmutableArray,
                token);
        }

        public ValueTask<ImmutableDictionary<AchievementId, Model.Metadata.Achievement.Achievement>> GetIdToAchievementMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<AchievementId, Model.Metadata.Achievement.Achievement>(MetadataFileStrategies.Achievement, token);
        }

        public ValueTask<ImmutableDictionary<AvatarId, Avatar>> GetIdToAvatarMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<AvatarId, Avatar>(MetadataFileStrategies.Avatar, token);
        }

        public ValueTask<ImmutableDictionary<PromoteId, ImmutableDictionary<PromoteLevel, Promote>>> GetIdToAvatarPromoteGroupMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<Promote, IGrouping<PromoteId, Promote>, PromoteId, ImmutableDictionary<PromoteLevel, Promote>>(
                MetadataFileStrategies.AvatarPromote,
                array => array.GroupBy(p => p.Id),
                g => g.Key,
                g => g.ToImmutableDictionary(p => p.Level),
                token);
        }

        public async ValueTask<ImmutableDictionary<MaterialId, DisplayItem>> GetIdToDisplayItemAndMaterialMapAsync(CancellationToken token = default)
        {
            const string CacheKey = $"{nameof(MetadataService)}.Cache.{nameof(MetadataFileStrategies.DisplayItem)}+{nameof(MetadataFileStrategies.Material)}.Map.{nameof(MaterialId)}.{nameof(DisplayItem)}+{nameof(Material)}";
            ImmutableDictionary<MaterialId, DisplayItem>? result = await metadataService.MemoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                ImmutableDictionary<MaterialId, DisplayItem> displays = await metadataService.FromCacheAsDictionaryAsync<MaterialId, DisplayItem>(MetadataFileStrategies.DisplayItem, a => a.Id, token).ConfigureAwait(false);
                ImmutableDictionary<MaterialId, Material> materials = await metadataService.GetIdToMaterialMapAsync(token).ConfigureAwait(false);

                ImmutableDictionary<MaterialId, DisplayItem>.Builder results = ImmutableDictionary.CreateBuilder<MaterialId, DisplayItem>();
                results.AddRange(displays);

                foreach ((MaterialId id, DisplayItem material) in materials)
                {
                    results[id] = material;
                }

                return results.ToImmutable();
            }).ConfigureAwait(false);

            ArgumentNullException.ThrowIfNull(result);
            return result;
        }

        public ValueTask<ImmutableDictionary<HardChallengeScheduleId, HardChallengeSchedule>> GetIdToHardChallengeScheduleMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<HardChallengeScheduleId, HardChallengeSchedule>(MetadataFileStrategies.HardChallengeSchedule, token);
        }

        public ValueTask<ImmutableDictionary<HyperLinkNameId, HyperLinkName>> GetIdToHyperLinkNameMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<HyperLinkNameId, HyperLinkName>(MetadataFileStrategies.HyperLinkName, token);
        }

        public ValueTask<ImmutableDictionary<MaterialId, Material>> GetIdToMaterialMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<MaterialId, Material>(MetadataFileStrategies.Material, token);
        }

        public ValueTask<ImmutableDictionary<ReliquaryId, Reliquary>> GetIdToReliquaryMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<Reliquary, ReliquaryId, Reliquary>(
                MetadataFileStrategies.Reliquary,
                array => array.SelectMany(r => r.Ids, (r, i) => (Index: i, Reliquary: r)),
                token);
        }

        public ValueTask<ImmutableDictionary<ReliquaryMainAffixId, FightProperty>> GetIdToReliquaryMainPropertyMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquaryMainAffix, ReliquaryMainAffixId, FightProperty>(
                MetadataFileStrategies.ReliquaryMainAffix,
                r => r.Id,
                r => r.Type,
                token);
        }

        public ValueTask<ImmutableDictionary<ReliquarySetId, ReliquarySet>> GetIdToReliquarySetMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquarySetId, ReliquarySet>(
                MetadataFileStrategies.ReliquarySet,
                r => r.SetId,
                token);
        }

        public ValueTask<ImmutableDictionary<ReliquarySubAffixId, ReliquarySubAffix>> GetIdToReliquarySubAffixMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<ReliquarySubAffixId, ReliquarySubAffix>(MetadataFileStrategies.ReliquarySubAffix, token);
        }

        public ValueTask<ImmutableDictionary<RoleCombatScheduleId, RoleCombatSchedule>> GetIdToRoleCombatScheduleMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<RoleCombatScheduleId, RoleCombatSchedule>(MetadataFileStrategies.RoleCombatSchedule, token);
        }

        public ValueTask<ImmutableDictionary<TowerFloorId, TowerFloor>> GetIdToTowerFloorMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<TowerFloorId, TowerFloor>(MetadataFileStrategies.TowerFloor, token);
        }

        public ValueTask<ImmutableDictionary<TowerScheduleId, TowerSchedule>> GetIdToTowerScheduleMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<TowerScheduleId, TowerSchedule>(MetadataFileStrategies.TowerSchedule, token);
        }

        public ValueTask<ImmutableDictionary<WeaponId, Weapon>> GetIdToWeaponMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<WeaponId, Weapon>(MetadataFileStrategies.Weapon, token);
        }

        public ValueTask<ImmutableDictionary<PromoteId, ImmutableDictionary<PromoteLevel, Promote>>> GetIdToWeaponPromoteGroupMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<Promote, IGrouping<PromoteId, Promote>, PromoteId, ImmutableDictionary<PromoteLevel, Promote>>(
                MetadataFileStrategies.WeaponPromote,
                array => array.GroupBy(p => p.Id),
                g => g.Key,
                g => g.ToImmutableDictionary(p => p.Level),
                token);
        }

        public ValueTask<ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>>> GetLevelToAvatarCurveMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<GrowCurve, Level, TypeValueCollection<GrowCurveType, float>>(
                MetadataFileStrategies.AvatarCurve,
                a => a.Level,
                a => a.Curves,
                token);
        }

        public ValueTask<ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>>> GetLevelToMonsterCurveMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<GrowCurve, Level, TypeValueCollection<GrowCurveType, float>>(
                MetadataFileStrategies.MonsterCurve,
                m => m.Level,
                m => m.Curves,
                token);
        }

        public ValueTask<ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>>> GetLevelToWeaponCurveMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<GrowCurve, Level, TypeValueCollection<GrowCurveType, float>>(
                MetadataFileStrategies.WeaponCurve,
                w => w.Level,
                w => w.Curves,
                token);
        }

        public ValueTask<ImmutableDictionary<string, Avatar>> GetNameToAvatarMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<string, Avatar>(
                MetadataFileStrategies.Avatar,
                a => a.Name,
                token);
        }

        public ValueTask<ImmutableDictionary<string, Weapon>> GetNameToWeaponMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<string, Weapon>(
                MetadataFileStrategies.Weapon,
                w => w.Name,
                token);
        }

        public ValueTask<ImmutableDictionary<MonsterDescribeId, Monster>> GetDescribeIdToMonsterMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<MonsterDescribeId, Monster>(
                MetadataFileStrategies.Monster,
                m => m.DescribeId,
                token);
        }

        public ValueTask<ImmutableDictionary<MaterialId, Combine>> GetResultMaterialIdToCombineMapAsync(CancellationToken token = default)
        {
            return metadataService.FromCacheAsDictionaryAsync<MaterialId, Combine>(
                MetadataFileStrategies.Combine,
                c => c.Result.Id,
                token);
        }

        private ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TKey, TValue>(MetadataFileStrategy strategy, CancellationToken token)
            where TKey : notnull
            where TValue : class, IDefaultIdentity<TKey>
        {
            return FromCacheAsDictionaryAsync<TKey, TValue>(metadataService, strategy, v => v.Id, token);
        }

        private async ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TKey, TValue>(MetadataFileStrategy strategy, Func<TValue, TKey> keySelector, CancellationToken token)
            where TKey : notnull
            where TValue : class
        {
            string keyName = TypeNameHelper.GetTypeDisplayName(typeof(TKey));
            string valueName = TypeNameHelper.GetTypeDisplayName(typeof(TValue));
            string cacheKey = $"{nameof(MetadataService)}.Cache.{strategy.Name}.Map.{keyName}.{valueName}";

            ImmutableDictionary<TKey, TValue>? result = await metadataService.MemoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                ImmutableArray<TValue> array = await metadataService.FromCacheOrFileAsync<TValue>(strategy, token).ConfigureAwait(false);
                return array.ToImmutableDictionaryIgnoringDuplicateKeys(keySelector); // There are duplicate name items
            }).ConfigureAwait(false);

            ArgumentNullException.ThrowIfNull(result);
            return result;
        }

        private async ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TData, TKey, TValue>(MetadataFileStrategy strategy, Func<TData, TKey> keySelector, Func<TData, TValue> valueSelector, CancellationToken token)
            where TKey : notnull
            where TData : class
        {
            string keyName = TypeNameHelper.GetTypeDisplayName(typeof(TKey));
            string valueName = TypeNameHelper.GetTypeDisplayName(typeof(TValue));
            string cacheKey = $"{nameof(MetadataService)}.Cache.{strategy.Name}.Map.{keyName}.{valueName}";

            ImmutableDictionary<TKey, TValue>? result = await metadataService.MemoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                ImmutableArray<TData> array = await metadataService.FromCacheOrFileAsync<TData>(strategy, token).ConfigureAwait(false);
                return array.ToImmutableDictionaryIgnoringDuplicateKeys(keySelector, valueSelector); // There are duplicate name items
            }).ConfigureAwait(false);

            ArgumentNullException.ThrowIfNull(result);
            return result;
        }

        private ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TData, TKey, TValue>(MetadataFileStrategy strategy, Func<ImmutableArray<TData>, IEnumerable<(TKey Key, TValue Value)>> transform, CancellationToken token)
            where TKey : notnull
            where TData : class
        {
            return FromCacheAsDictionaryAsync(metadataService, strategy, transform, kvp => kvp.Key, kvp => kvp.Value, token);
        }

        private async ValueTask<ImmutableDictionary<TKey, TValue>> FromCacheAsDictionaryAsync<TData, TMiddle, TKey, TValue>(MetadataFileStrategy strategy, Func<ImmutableArray<TData>, IEnumerable<TMiddle>> transform, Func<TMiddle, TKey> keySelector, Func<TMiddle, TValue> valueSelector, CancellationToken token)
            where TKey : notnull
            where TData : class
        {
            string keyName = TypeNameHelper.GetTypeDisplayName(typeof(TKey));
            string middleName = TypeNameHelper.GetTypeDisplayName(typeof(TMiddle));
            string valueName = TypeNameHelper.GetTypeDisplayName(typeof(TValue));
            string cacheKey = $"{nameof(MetadataService)}.Cache.{strategy.Name}.Map.{keyName}.{middleName}.{valueName}";

            ImmutableDictionary<TKey, TValue>? result = await metadataService.MemoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                ImmutableArray<TData> array = await metadataService.FromCacheOrFileAsync<TData>(strategy, token).ConfigureAwait(false);
                return transform(array).ToImmutableDictionaryIgnoringDuplicateKeys(keySelector, valueSelector); // There are duplicate name items
            }).ConfigureAwait(false);

            ArgumentNullException.ThrowIfNull(result);
            return result;
        }
    }
}