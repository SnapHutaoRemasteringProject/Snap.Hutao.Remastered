// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Frozen;

namespace Snap.Hutao.Remastered.Service.HardChallenge;

internal interface IHardChallengeRepository : IRepository<HardChallengeEntry>
{
    void AddHardChallengeEntry(HardChallengeEntry entry);

    FrozenDictionary<uint, HardChallengeEntry> GetHardChallengeMapByUid(string uid);

    void UpdateHardChallengeEntry(HardChallengeEntry entry);
}