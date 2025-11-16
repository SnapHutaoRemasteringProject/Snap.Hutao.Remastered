// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Model.Metadata.Avatar;

internal static class IdLevelParametersCollectionExtension
{
    extension(IdLevelParametersCollection<ProudSkillId, SkillLevel, float> collection)
    {
        public ImmutableArray<LevelParameters<string, ParameterDescription>> Convert(ImmutableArray<string> descriptions, Func<ImmutableArray<string>, ImmutableArray<float>, ImmutableArray<ParameterDescription>> parameterDescriptionFactory)
        {
            ImmutableArray<LevelParameters<string, ParameterDescription>> parameters =
            [
                .. collection
                    .LevelParameters
                    .Select(param => new LevelParameters<string, ParameterDescription>(LevelFormat.Format(param.Key), parameterDescriptionFactory(descriptions, param.Value)))
            ];

            return parameters;
        }
    }
}