// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

internal sealed class DescriptionsParameters
{
    public required ImmutableArray<string> Descriptions { get; init; }

    public required IdLevelParametersCollection<ProudSkillId, SkillLevel, float> Parameters { get; init; }
}