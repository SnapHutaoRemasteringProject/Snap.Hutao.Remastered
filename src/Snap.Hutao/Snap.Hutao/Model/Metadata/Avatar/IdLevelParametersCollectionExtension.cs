// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Model.Metadata.Avatar;

internal static class IdLevelParametersCollectionExtension
{
    public static ImmutableArray<LevelParameters<string, ParameterDescription>> Convert(this IdLevelParametersCollection<ProudSkillId, SkillLevel, float> collection, ImmutableArray<string> descriptions, Func<ImmutableArray<string>, ImmutableArray<float>, ImmutableArray<ParameterDescription>> parameterDescriptionFactory)
    {
        ImmutableArray<LevelParameters<string, ParameterDescription>> parameters = collection
            .LevelParameters
            .Select(param => new LevelParameters<string, ParameterDescription>(LevelFormat.Format(param.Key), parameterDescriptionFactory(descriptions, param.Value)))
            .ToImmutableArray();

        return parameters;
    }
}