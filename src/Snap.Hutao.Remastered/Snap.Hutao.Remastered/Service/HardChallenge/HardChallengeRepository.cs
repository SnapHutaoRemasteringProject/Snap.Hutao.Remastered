// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Frozen;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.HardChallenge;

[Service(ServiceLifetime.Singleton, typeof(IHardChallengeRepository))]
public sealed partial class HardChallengeRepository : IHardChallengeRepository
{
    [GeneratedConstructor]
    public partial HardChallengeRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public FrozenDictionary<uint, HardChallengeEntry> GetHardChallengeMapByUid(string uid)
    {
        return this.WithDatabase(context => AppDbQueries.HardChallengeByUid(context, uid)).ToFrozenDictionary(e => e.ScheduleId);
    }

    public void UpdateHardChallengeEntry(HardChallengeEntry entry)
    {
        this.Update(entry);
    }

    public void AddHardChallengeEntry(HardChallengeEntry entry)
    {
        this.Add(entry);
    }
}