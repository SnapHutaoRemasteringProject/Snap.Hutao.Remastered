// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.Cultivation;

[Service(ServiceLifetime.Singleton, typeof(ICultivationRepository))]
public sealed partial class CultivationRepository : ICultivationRepository
{
    [GeneratedConstructor]
    public partial CultivationRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public ImmutableArray<CultivateEntry> GetCultivateEntryImmutableArrayByProjectId(Guid projectId)
    {
        return this.WithDatabase(context => AppDbQueries.CultivateEntriesByProject(context, projectId)).ToImmutableArray();
    }

    public ImmutableArray<CultivateEntry> GetCultivateEntryImmutableArrayIncludingLevelInformationByProjectId(Guid projectId)
    {
        return this.WithDatabase(context => AppDbQueries.CultivateEntriesWithLevels(context, projectId)).ToImmutableArray();
    }

    public ImmutableArray<CultivateItem> GetCultivateItemImmutableArrayByEntryId(Guid entryId)
    {
        return this.WithDatabase(context => AppDbQueries.CultivateItemsByEntry(context, entryId)).ToImmutableArray();
    }

    public void RemoveCultivateEntryById(Guid entryId)
    {
        this.DeleteByInnerId<CultivateEntry>(entryId);
    }

    public bool UpdateCultivateItem(CultivateItem item)
    {
        // A pending card command can outlive deletion/replacement of its row.
        // Update only the finish flag and report a missing row without attaching
        // the stale entity as Modified or recreating it.
        return this.WithDatabase(context => AppDbQueries.UpdateCultivateItemFinished(context, item.InnerId, item.IsFinished)) == 1;
    }

    public ImmutableArray<CultivateEntry> GetCultivateEntryImmutableArrayByProjectIdAndItemId(Guid projectId, uint itemId)
    {
        return this.WithDatabase(context => AppDbQueries.CultivateEntriesByProjectAndItem(context, projectId, itemId)).ToImmutableArray();
    }

    public void AddCultivateEntry(CultivateEntry entry)
    {
        this.Add(entry);
    }

    public void RemoveCultivateItemRangeByEntryId(Guid entryId)
    {
        this.WithDatabase(context => AppDbQueries.DeleteCultivateItemsByEntry(context, entryId));
    }

    public void AddCultivateItemRange(IEnumerable<CultivateItem> toAdd)
    {
        this.AddRange(toAdd);
    }

    public void AddCultivateProject(CultivateProject project)
    {
        this.Add(project);
    }

    public void RemoveCultivateProjectById(Guid projectId)
    {
        this.DeleteByInnerId<CultivateProject>(projectId);
    }

    public ObservableCollection<CultivateProject> GetCultivateProjectCollection()
    {
        return this.ObservableCollection<CultivateProject>();
    }

    public void RemoveLevelInformationByEntryId(Guid entryId)
    {
        this.WithDatabase(context => AppDbQueries.DeleteLevelsByEntry(context, entryId));
    }

    public void AddLevelInformation(CultivateEntryLevelInformation levelInformation)
    {
        this.Add(levelInformation);
    }

    public CultivateProject? GetCultivateProjectById(Guid projectId)
    {
        return this.WithDatabase(context => AppDbQueries.CultivateProjectById(context, projectId));
    }

    public Guid GetCultivateProjectIdByEntryId(Guid entryId)
    {
        return this.WithDatabase(context => AppDbQueries.CultivateProjectIdByEntry(context, entryId));
    }
}
