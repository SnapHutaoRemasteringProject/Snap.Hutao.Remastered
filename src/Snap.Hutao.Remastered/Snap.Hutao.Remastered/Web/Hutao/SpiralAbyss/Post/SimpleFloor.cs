// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.SpiralAbyss;

namespace Snap.Hutao.Remastered.Web.Hutao.SpiralAbyss.Post;

internal sealed class SimpleFloor
{
    public SimpleFloor(SpiralAbyssFloor floor)
    {
        Index = floor.Index;
        Star = floor.Star;
        Levels = floor.Levels.Select(l => new SimpleLevel(l));
    }

    public uint Index { get; set; }

    public int Star { get; set; }

    public IEnumerable<SimpleLevel> Levels { get; set; }
}