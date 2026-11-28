// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class IdLevelParameters<TId, TLevel, TParam>
{
    public IdLevelParameters()
    {
    }

    public IdLevelParameters(TId id, TLevel level, ImmutableArray<TParam> parameters)
    {
        Id = id;
        Level = level;
        Parameters = parameters;
    }

    [ProtoMember(1, IsRequired = true)]
    public TId Id { get; set; } = default!;

    [ProtoMember(2, IsRequired = true)]
    public TLevel Level { get; set; } = default!;

    [ProtoMember(3)]
    public ImmutableArray<TParam> Parameters { get; set; } = [];
}
