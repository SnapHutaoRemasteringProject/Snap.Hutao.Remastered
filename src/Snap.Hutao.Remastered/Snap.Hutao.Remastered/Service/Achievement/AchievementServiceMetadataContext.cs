// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;
using System.Collections.Immutable;
using MetadataAchievement = Snap.Hutao.Remastered.Model.Metadata.Achievement.Achievement;

namespace Snap.Hutao.Remastered.Service.Achievement;

internal sealed class AchievementServiceMetadataContext : IMetadataContext,
    IMetadataArrayAchievementSource,
    IMetadataDictionaryIdAchievementSource
{
    public ImmutableArray<MetadataAchievement> Achievements { get; set; } = default!;

    public ImmutableDictionary<AchievementId, MetadataAchievement> IdAchievementMap { get; set; } = default!;
}