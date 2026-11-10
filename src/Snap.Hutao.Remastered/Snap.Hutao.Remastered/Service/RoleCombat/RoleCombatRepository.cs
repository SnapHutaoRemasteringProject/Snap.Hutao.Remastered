// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Frozen;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.RoleCombat;

[Service(ServiceLifetime.Singleton, typeof(IRoleCombatRepository))]
public sealed partial class RoleCombatRepository : IRoleCombatRepository
{
    [GeneratedConstructor]
    public partial RoleCombatRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public FrozenDictionary<uint, RoleCombatEntry> GetRoleCombatEntryMapByUid(string uid)
    {
        return this.WithDatabase(context => AppDbQueries.RoleCombatByUid(context, uid)).ToFrozenDictionary(e => e.ScheduleId);
    }

    public void UpdateRoleCombatEntry(RoleCombatEntry entry)
    {
        this.Update(entry);
    }

    public void AddRoleCombatEntry(RoleCombatEntry entry)
    {
        this.Add(entry);
    }
}