// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

internal class IdCount
{
    public required MaterialId Id { get; init; }

    public required uint Count { get; init; }
}