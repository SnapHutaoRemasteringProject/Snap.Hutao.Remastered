// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// One proto category adapter: knows which application model it produces and maps the category out
/// of one <see cref="LanguageMetadata"/>.
/// </summary>
internal interface IProtoCategoryAdapter
{
    MetadataCategory Category { get; }

    Type ElementType { get; }

    /// <summary>
    /// Maps one category of <paramref name="language"/> into the application model array.
    /// </summary>
    object Read(LanguageMetadata language, MetadataConvert convert);
}
