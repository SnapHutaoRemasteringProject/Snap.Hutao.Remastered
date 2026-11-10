// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Frozen;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.SpiralAbyss;

[Service(ServiceLifetime.Singleton, typeof(ISpiralAbyssRepository))]
public sealed partial class SpiralAbyssRepository : ISpiralAbyssRepository
{
    [GeneratedConstructor]
    public partial SpiralAbyssRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public FrozenDictionary<uint, SpiralAbyssEntry> GetSpiralAbyssEntryMapByUid(string uid)
    {
        return this.WithDatabase(context => AppDbQueries.SpiralAbyssByUid(context, uid)).ToFrozenDictionary(e => e.ScheduleId);
    }

    public void UpdateSpiralAbyssEntry(SpiralAbyssEntry entry)
    {
        this.Update(entry);
    }

    public void AddSpiralAbyssEntry(SpiralAbyssEntry entry)
    {
        this.Add(entry);
    }
}