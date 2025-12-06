// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.GachaLog.QueryProvider;
using Snap.Hutao.Remastered.ViewModel.GachaLog;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.GachaLog;

internal interface IGachaLogService
{
    ValueTask<GachaArchive> EnsureArchiveInCollectionAsync(Guid archiveId, CancellationToken token = default);

    ValueTask<GachaStatistics> GetStatisticsAsync(GachaLogServiceMetadataContext context, GachaArchive archive);

    ValueTask<ImmutableArray<GachaStatisticsSlim>> GetStatisticsSlimImmutableArrayAsync(GachaLogServiceMetadataContext context, CancellationToken token = default);

    ValueTask<bool> RefreshGachaLogAsync(GachaLogServiceMetadataContext context, GachaLogQuery query, RefreshStrategyKind strategy, IProgress<GachaLogFetchStatus> progress, CancellationToken token);

    ValueTask RemoveArchiveAsync(GachaArchive archive);

    ValueTask<IAdvancedDbCollectionView<GachaArchive>> GetArchiveCollectionAsync();
}