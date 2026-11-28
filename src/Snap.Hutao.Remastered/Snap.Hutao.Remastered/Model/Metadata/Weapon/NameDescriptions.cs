// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

[ProtoContract]
public sealed class NameDescriptions
{
    [ProtoMember(1)]
    public string Name { get; init; }

    [ProtoMember(2)]
    public ImmutableArray<LevelDescription> Descriptions { get; init; } = [];
}
