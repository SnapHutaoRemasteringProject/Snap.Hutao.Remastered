// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Calculable;

internal interface ICalculablePromoteLevel : ICalculableMinMaxLevel
{
    PromoteLevel PromoteLevel { get; }

    bool IsPromoted { get; }

    bool IsPromotionAvailable { get; }
}