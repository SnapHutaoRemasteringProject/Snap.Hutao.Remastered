// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Service.AvatarInfo.Factory.Builder;
using Snap.Hutao.Remastered.ViewModel.AvatarProperty;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;
using System.Collections.Immutable;
using DetailedReliquary = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar.Reliquary;
using MetadataReliquary = Snap.Hutao.Remastered.Model.Metadata.Reliquary.Reliquary;
using MetadataReliquarySet = Snap.Hutao.Remastered.Model.Metadata.Reliquary.ReliquarySet;

namespace Snap.Hutao.Remastered.Service.AvatarInfo.Factory;

public sealed class SummaryReliquaryFactory
{
    private readonly SummaryFactoryMetadataContext metadataContext;
    private readonly DetailedReliquary reliquary;
    private readonly ImmutableArray<FightProperty> recommendedSubProperties;
    private readonly EnergyType energyType;
    private readonly bool isCritEffective;

    public SummaryReliquaryFactory(
        SummaryFactoryMetadataContext metadataContext,
        DetailedReliquary reliquary,
        ImmutableArray<FightProperty> recommendedSubProperties,
        EnergyType energyType,
        bool isCritEffective)
    {
        this.metadataContext = metadataContext;
        this.reliquary = reliquary;
        this.recommendedSubProperties = recommendedSubProperties;
        this.energyType = energyType;
        this.isCritEffective = isCritEffective;
    }

    public static ReliquaryView Create(
        SummaryFactoryMetadataContext metadataContext,
        DetailedReliquary reliquary,
        ImmutableArray<FightProperty> recommendedSubProperties,
        EnergyType energyType,
        bool isCritEffective)
    {
        return new SummaryReliquaryFactory(metadataContext, reliquary, recommendedSubProperties, energyType, isCritEffective).Create();
    }

    public ReliquaryView Create()
    {
        MetadataReliquary metaReliquary = metadataContext.IdReliquaryMap[reliquary.Id];
        MetadataReliquarySet metaReliquarySet = metadataContext.IdReliquarySetMap[metaReliquary.SetId];

        double score = ReliquaryScoreCalculator.Calculate(recommendedSubProperties, reliquary.SubPropertyList, energyType, isCritEffective);

        ReliquaryViewBuilder reliquaryViewBuilder = new ReliquaryViewBuilder()
            .SetName(metaReliquary.Name)
            .SetIcon(RelicIconConverter.IconNameToUri(metaReliquary.Icon))
            .SetDescription(metaReliquary.Description)
            .SetLevel($"+{reliquary.Level}")
            .SetQuality(metaReliquary.RankLevel)
            .SetEquipType(metaReliquary.EquipType)
            .SetSetName(metaReliquarySet.Name)
            .SetMainProperty(FightPropertyFormat.ToNameValue(reliquary.MainProperty))
            .SetComposedSubProperties(reliquary.SubPropertyList.SelectAsArray(CreateSubProperty))
            .SetScoreValue(score)
            .SetScore(string.Format(SH.ViewPageAvatarPropertyReliquaryScoreValue, score));

        return reliquaryViewBuilder.View;
    }

    private static ReliquaryComposedSubProperty CreateSubProperty(ReliquaryProperty property)
    {
        return new(property.PropertyType, property.Value, property.Times + 1);
    }
}
