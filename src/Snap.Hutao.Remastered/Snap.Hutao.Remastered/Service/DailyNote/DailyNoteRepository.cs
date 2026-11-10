// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.DailyNote;

[Service(ServiceLifetime.Singleton, typeof(IDailyNoteRepository))]
public sealed partial class DailyNoteRepository : IDailyNoteRepository
{
    [GeneratedConstructor]
    public partial DailyNoteRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public bool ContainsUid(string uid)
    {
        return this.WithDatabase(context => AppDbQueries.DailyNoteContainsUid(context, uid));
    }

    public void AddDailyNoteEntry(DailyNoteEntry entry)
    {
        this.Add(entry);
    }

    public void DeleteDailyNoteEntryById(Guid entryId)
    {
        this.DeleteByInnerId(entryId);
    }

    public void UpdateDailyNoteEntry(DailyNoteEntry entry)
    {
        this.Update(entry);
    }

    public ImmutableArray<DailyNoteEntry> GetDailyNoteEntryImmutableArrayIncludingUser()
    {
        return this.WithDatabase(context => AppDbQueries.DailyNotesIncludingUser(context)).ToImmutableArray();
    }
}