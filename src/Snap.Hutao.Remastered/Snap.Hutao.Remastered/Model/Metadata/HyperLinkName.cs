// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

internal sealed class HyperLinkName : IDefaultIdentity<HyperLinkNameId>
{
    public required HyperLinkNameId Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}