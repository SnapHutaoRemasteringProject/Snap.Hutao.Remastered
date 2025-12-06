// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model.Calculable;

internal interface ICalculable : INameIcon<Uri>
{
    QualityType Quality { get; }

    uint LevelCurrent { get; set; }

    uint LevelTarget { get; set; }
}