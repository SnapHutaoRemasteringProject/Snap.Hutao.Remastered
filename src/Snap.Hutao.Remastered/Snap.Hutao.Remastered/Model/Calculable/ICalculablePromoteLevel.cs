// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Calculable;

public interface ICalculablePromoteLevel : ICalculableMinMaxLevel
{
    PromoteLevel PromoteLevel { get; }

    bool IsPromoted { get; set; }

    bool IsPromotionAvailable { get; }
}
