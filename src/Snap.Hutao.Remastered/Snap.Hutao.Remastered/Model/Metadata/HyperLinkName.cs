// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class HyperLinkName : IDefaultIdentity<HyperLinkNameId>
{
    [ProtoMember(1)]
    public HyperLinkNameId Id { get; init; }

    [ProtoMember(2)]
    public string Name { get; init; }

    [ProtoMember(3)]
    public string Description { get; init; }
}
