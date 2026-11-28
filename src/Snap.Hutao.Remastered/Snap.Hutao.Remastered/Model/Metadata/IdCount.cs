// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
[ProtoInclude(100, typeof(Snap.Hutao.Remastered.Model.Metadata.Achievement.Reward))]
public class IdCount
{
    [ProtoMember(1)]
    public MaterialId Id { get; init; }

    [ProtoMember(2)]
    public uint Count { get; init; }
}
