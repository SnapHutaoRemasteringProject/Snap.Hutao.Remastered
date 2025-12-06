// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Reliquary;

internal class ReliquaryMainAffix
{
    public required ReliquaryMainAffixId Id { get; init; }

    public required FightProperty Type { get; init; }
}