// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed class ExtraLevelInfo
{
    [ProtoMember(1)]
    public ExtraLevelIndexKind Index { get; init; }

    [ProtoMember(2)]
    public uint Level { get; init; }
}
