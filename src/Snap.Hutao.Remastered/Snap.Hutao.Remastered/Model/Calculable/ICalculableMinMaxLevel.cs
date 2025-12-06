// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Calculable;

internal interface ICalculableMinMaxLevel : ICalculable
{
    uint LevelMin { get; }

    uint LevelMax { get; }
}