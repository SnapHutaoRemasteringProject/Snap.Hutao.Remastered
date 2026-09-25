// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Service.Backpack;
using System.Collections.Immutable;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public sealed class BackpackReliquaryItemView : BackpackItemView
{
    public Reliquary Reliquary { get; private set; } = default!;

    public string? SetName { get; private set; }

    public Uri? SetIconUri { get; private set; }

    public string EquipTypeName => Reliquary.EquipType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!;

    public uint Level => Entity.Level > 0 ? Entity.Level - 1 : 0;

    public bool IsLocked => Entity.IsLocked;

    public bool IsMarked => Entity.IsMarked;

    public string? MainPropName { get; private set; }

    public string? MainPropValue { get; private set; }

    public double Score { get; set; }

    public bool HasScore => Score > 0;

    public string DisplayScore => HasScore ? string.Format(CultureInfo.CurrentCulture, SH.ViewPageBackpackReliquaryScoreValue, Score) : string.Empty;

    public int ScoreColorValue => (int)Math.Round(Score);

    public ImmutableArray<BackpackReliquarySubStatView> SubStats { get; private set; } = [];

    /// <summary>
    /// Always returns at least 4 entries, padding with empty placeholders.
    /// Computed once in <see cref="Create"/> to avoid per-access allocations from XAML bindings.
    /// </summary>
    public ImmutableArray<BackpackReliquarySubStatView> PaddedSubStats { get; private set; } = [];

    public static BackpackReliquaryItemView Create(BackpackItem entity, BackpackServiceMetadataContext context, Reliquary reliquary)
    {
        BackpackReliquaryItemView view = new()
        {
            Entity = entity,
            Category = BackpackItemCategory.Reliquary,
            Reliquary = reliquary,
            Name = reliquary.Name,
            Description = reliquary.Description,
            TypeDescription = reliquary.EquipType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!,
            IconUri = ItemIconConverter.IconNameToUri(reliquary.Icon),
            Quality = reliquary.RankLevel,
        };

        if (context.IdReliquarySetMap.TryGetValue(reliquary.SetId, out ReliquarySet? set))
        {
            view.SetName = set.Name;
            view.SetIconUri = RelicIconConverter.IconNameToUri(set.Icon);

            ImmutableArray<int> needNumbers = set.NeedNumber;
            ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(set.Descriptions.Length);
            for (int i = 0; i < set.Descriptions.Length; i++)
            {
                string label = i < needNumbers.Length
                    ? SH.FormatViewPageBackpackSetEffectLabel(needNumbers[i])
                    : string.Empty;
                builder.Add($"{label}{set.Descriptions[i]}");
            }

            view.SetDescriptions = builder.MoveToImmutable();
        }

        FightProperty? mainFightProp = null;
        if (entity.MainPropId is { } mainPropId)
        {
            if (context.IdReliquaryMainPropertyMap.TryGetValue(mainPropId, out FightProperty fp))
            {
                view.MainPropName = fp.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture);
                mainFightProp = fp;
            }
        }

        // Resolve main stat value from growth table
        if (mainFightProp is { } prop)
        {
            foreach (ref readonly ReliquaryMainAffixLevel level in context.ReliquaryMainAffixLevels.AsSpan())
            {
                if (level.Rank == reliquary.RankLevel && level.Level == entity.Level)
                {
                    if (level.Properties.GetValueOrDefault(prop) is float value and not 0)
                    {
                        view.MainPropValue = prop.IsFightPropPercent()
                            ? value.ToString("P1", CultureInfo.CurrentCulture)
                            : value.ToString("F0", CultureInfo.CurrentCulture);
                    }

                    break;
                }
            }
        }

        view.BuildSubStats(context);

        // Cache padded sub-stats to avoid allocations on every XAML binding access
        int count = view.SubStats.Length;
        if (count >= 4)
        {
            view.PaddedSubStats = view.SubStats;
        }
        else
        {
            ImmutableArray<BackpackReliquarySubStatView>.Builder builder = ImmutableArray.CreateBuilder<BackpackReliquarySubStatView>(4);
            builder.AddRange(view.SubStats);
            for (int i = count; i < 4; i++)
            {
                builder.Add(BackpackReliquarySubStatView.Empty);
            }

            view.PaddedSubStats = builder.MoveToImmutable();
        }

        view.FillEquippedAvatar(context);

        return view;
    }

    private void BuildSubStats(BackpackServiceMetadataContext context)
    {
        // Resolve IDs to FightProp+Value pairs, maintaining order
        List<(FightProperty Prop, float Value)> resolved = ResolveSubAffixes(context, Entity.AppendPropIdListJson, nameof(Entity.AppendPropIdListJson));
        if (resolved.Count == 0)
        {
            return;
        }

        // Merge same FightProp: first occurrence = initial, subsequent = upgrades
        Dictionary<FightProperty, (float TotalValue, uint EnhancedCount)> merged = [];
        HashSet<FightProperty> seen = [];
        foreach ((FightProperty prop, float value) in resolved)
        {
            if (seen.Add(prop))
            {
                merged[prop] = (value, 0);
            }
            else
            {
                (float total, uint count) = merged[prop];
                merged[prop] = (total + value, count + 1);
            }
        }

        ImmutableArray<BackpackReliquarySubStatView>.Builder builder = ImmutableArray.CreateBuilder<BackpackReliquarySubStatView>();
        HashSet<FightProperty> added = [];
        foreach ((FightProperty prop, float _) in resolved)
        {
            if (added.Add(prop))
            {
                (float totalValue, uint enhancedCount) = merged[prop];
                builder.Add(new BackpackReliquarySubStatView
                {
                    FightProp = prop,
                    Value = totalValue,
                    EnhancedCount = enhancedCount,
                    State = GetSubStatState(prop, context.ReliquaryScoreConfig),
                });
            }
        }

        // The fourth sub stat of an artifact that has not reached level 4 is already known, but not active yet
        foreach ((FightProperty prop, float value) in ResolveSubAffixes(context, Entity.DefiniteAppendPropIdListJson, nameof(Entity.DefiniteAppendPropIdListJson)))
        {
            if (added.Add(prop))
            {
                builder.Add(new BackpackReliquarySubStatView
                {
                    FightProp = prop,
                    Value = value,
                    State = ReliquarySubStatState.Inactive,
                });
            }
        }

        SubStats = builder.ToImmutable();
    }

    private static List<(FightProperty Prop, float Value)> ResolveSubAffixes(BackpackServiceMetadataContext context, string? json, string propertyName)
    {
        List<(FightProperty Prop, float Value)> resolved = [];
        if (DeserializeSubAffixIds(json, propertyName) is not { Length: > 0 } ids)
        {
            return resolved;
        }

        foreach (uint id in ids)
        {
            if (context.IdReliquarySubAffixMap.TryGetValue(id, out ReliquarySubAffix? subAffix))
            {
                resolved.Add((subAffix.Type, subAffix.Value));
            }
        }

        return resolved;
    }

    private static uint[]? DeserializeSubAffixIds(string? json, string propertyName)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<uint[]>(json);
        }
        catch (JsonException ex)
        {
            SentrySdk.AddBreadcrumb(
                message: $"Failed to deserialize {propertyName}: {ex.Message}",
                category: "BackpackReliquaryItemView",
                level: BreadcrumbLevel.Error);
            return null;
        }
    }

    private static ReliquarySubStatState GetSubStatState(FightProperty prop, BackpackReliquaryScoreConfig scoreConfig)
    {
        if (prop is FightProperty.FIGHT_PROP_CRITICAL or FightProperty.FIGHT_PROP_CRITICAL_HURT)
        {
            return ReliquarySubStatState.Crit;
        }

        return scoreConfig.GetWeight(prop) > 0 ? ReliquarySubStatState.Effective : ReliquarySubStatState.Ineffective;
    }
}
