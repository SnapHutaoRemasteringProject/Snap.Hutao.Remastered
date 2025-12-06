// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Frozen;

namespace Snap.Hutao.Remastered.Service.SpiralAbyss;

internal interface ISpiralAbyssRepository : IRepository<SpiralAbyssEntry>
{
    void AddSpiralAbyssEntry(SpiralAbyssEntry entry);

    FrozenDictionary<uint, SpiralAbyssEntry> GetSpiralAbyssEntryMapByUid(string uid);

    void UpdateSpiralAbyssEntry(SpiralAbyssEntry entry);
}