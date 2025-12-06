// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Cultivation.Consumption;
using Snap.Hutao.Remastered.ViewModel.Cultivation;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.Cultivation;

internal interface ICultivationService
{
    ValueTask<IAdvancedDbCollectionView<CultivateProject>> GetProjectCollectionAsync();

    ValueTask<bool> EnsureCurrentProjectAsync(IAdvancedDbCollectionView<CultivateProject> projects);

    ValueTask<ObservableCollection<CultivateEntryView>> GetCultivateEntryCollectionAsync(CultivateProject cultivateProject, ICultivationMetadataContext context);

    ValueTask<StatisticsCultivateItemCollection> GetStatisticsCultivateItemCollectionAsync(CultivateProject cultivateProject, ICultivationMetadataContext context, CancellationToken token);

    ValueTask<ResinStatistics> GetResinStatisticsAsync(StatisticsCultivateItemCollection statisticsCultivateItems, CancellationToken token);

    ValueTask RemoveCultivateEntryAsync(Guid entryId);

    ValueTask RemoveProjectAsync(CultivateProject project);

    ValueTask<ConsumptionSaveResultKind> SaveConsumptionAsync(InputConsumption inputConsumption);

    void SaveCultivateItem(CultivateItemView item);

    ValueTask<ProjectAddResultKind> TryAddProjectAsync(CultivateProject project);
}