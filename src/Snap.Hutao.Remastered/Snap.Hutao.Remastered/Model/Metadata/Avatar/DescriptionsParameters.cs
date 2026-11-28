// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed class DescriptionsParameters
{
    [ProtoMember(1)]
    public ImmutableArray<string> Descriptions { get; init; } = [];

    [ProtoMember(2)]
    public IdLevelParametersCollection<ProudSkillId, SkillLevel, float> Parameters { get; init; }

    [ProtoMember(3)]
    public int[]? DescriptionList { get; init; }

    [ProtoMember(4)]
    public int[]? SpecialDescriptionList { get; init; }
}
