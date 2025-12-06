// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.ViewModel.Complex;
using Snap.Hutao.Remastered.Web.Hutao.SpiralAbyss;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Hutao;

internal interface IHutaoSpiralAbyssStatisticsCache
{
    ImmutableArray<AvatarRankView> AvatarUsageRanks { get; set; }

    ImmutableArray<AvatarRankView> AvatarAppearanceRanks { get; set; }

    ImmutableArray<AvatarConstellationInfoView> AvatarConstellationInfos { get; set; }

    ImmutableArray<TeamAppearanceView> TeamAppearances { get; set; }

    Overview? Overview { get; set; }

    ImmutableDictionary<AvatarId, AvatarCollocationView>? AvatarCollocations { get; set; }

    ImmutableDictionary<WeaponId, WeaponCollocationView>? WeaponCollocations { get; set; }

    ValueTask InitializeForSpiralAbyssViewAsync(HutaoSpiralAbyssStatisticsMetadataContext context);

    ValueTask InitializeForWikiAvatarViewAsync(HutaoSpiralAbyssStatisticsMetadataContext context);

    ValueTask InitializeForWikiWeaponViewAsync(HutaoSpiralAbyssStatisticsMetadataContext context);
}