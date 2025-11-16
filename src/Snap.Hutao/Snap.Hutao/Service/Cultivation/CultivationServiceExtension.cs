// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Database;
using Snap.Hutao.Model.Entity;
using Snap.Hutao.ViewModel.Cultivation;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Service.Cultivation;

internal static class CultivationServiceExtension
{
    extension(ICultivationService cultivationService)
    {
        public async ValueTask<CultivateProject?> GetCurrentProjectAsync()
        {
            IAdvancedDbCollectionView<CultivateProject> projects = await cultivationService.GetProjectCollectionAsync().ConfigureAwait(false);
            await cultivationService.EnsureCurrentProjectAsync(projects).ConfigureAwait(false);
            return projects.CurrentItem;
        }

        public async ValueTask<ObservableCollection<CultivateEntryView>?> GetCultivateEntryCollectionForCurrentProjectAsync(ICultivationMetadataContext context)
        {
            IAdvancedDbCollectionView<CultivateProject> projects = await cultivationService.GetProjectCollectionAsync().ConfigureAwait(false);
            if (!await cultivationService.EnsureCurrentProjectAsync(projects).ConfigureAwait(false))
            {
                return default;
            }

            ArgumentNullException.ThrowIfNull(projects.CurrentItem);
            return await cultivationService.GetCultivateEntryCollectionAsync(projects.CurrentItem, context).ConfigureAwait(false);
        }
    }
}