// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata;

/// <summary>
/// One metadata category: the JSON contract name used by the git source, and the
/// <c>LanguageMetadata</c> message name used by the proto source.
/// </summary>
public sealed class MetadataCategory
{
    public MetadataCategory(string name, string? protoName = null, bool isScattered = false)
    {
        Name = name;
        ProtoName = protoName ?? name;
        IsScattered = isScattered;
    }

    /// <summary>
    /// JSON file name published by Snap.Metadata (<c>{Name}.json</c> or <c>{Name}/*.json</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Proto message name inside <c>LanguageMetadata</c>.
    /// </summary>
    public string ProtoName { get; }

    /// <summary>
    /// Whether the git source stores this category as one file per item instead of one array file.
    /// </summary>
    public bool IsScattered { get; }
}
