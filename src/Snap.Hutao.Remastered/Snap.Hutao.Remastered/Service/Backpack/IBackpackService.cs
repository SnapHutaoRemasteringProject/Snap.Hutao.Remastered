// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Service.Yae.PlayerStore;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Backpack;

public interface IBackpackService
{
    ValueTask<IAdvancedDbCollectionView<BackpackArchive>> GetArchiveCollectionAsync();

    bool RefreshByEmbeddedYae(BackpackArchive archive, PlayerStoreResult storeResult);

    void RemoveArchive(BackpackArchive archive);

    BackpackArchive AddArchive(string name);

    ImmutableArray<BackpackItem> GetBackpackItemImmutableArrayByArchiveId(Guid archiveId);

    BackpackReliquaryScoreConfig GetActiveReliquaryScoreConfig();

    ImmutableArray<BackpackReliquaryScoreConfig> GetAllReliquaryScoreConfigs();

    BackpackReliquaryScoreConfig CreatePreset(ReliquaryScoreConfigPreset preset);

    BackpackReliquaryScoreConfig SaveReliquaryScoreConfig(BackpackReliquaryScoreConfig config);

    void DeleteReliquaryScoreConfig(Guid configId);
}
