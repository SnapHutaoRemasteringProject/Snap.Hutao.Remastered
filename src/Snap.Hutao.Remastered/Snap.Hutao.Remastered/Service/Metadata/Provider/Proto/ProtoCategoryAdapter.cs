// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Wire;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Reads one category from the parsed bundle. A category is a single typed array on
/// <see cref="LanguageMetadata"/>, so the adapter only projects that array; there is no mapping
/// layer any more because the domain models are the protobuf-net wire contract.
/// </summary>
internal interface IProtoCategoryAdapter
{
    MetadataCategory Category { get; }

    Type ElementType { get; }

    object Read(LanguageMetadata language);
}

internal sealed class ProtoCategoryAdapter<T> : IProtoCategoryAdapter
{
    private readonly Func<LanguageMetadata, ImmutableArray<T>> read;

    public ProtoCategoryAdapter(MetadataCategory category, Func<LanguageMetadata, ImmutableArray<T>> read)
    {
        Category = category;
        this.read = read;
    }

    public MetadataCategory Category { get; }

    public Type ElementType
    {
        get => typeof(T);
    }

    public object Read(LanguageMetadata language)
    {
        return read(language);
    }
}
