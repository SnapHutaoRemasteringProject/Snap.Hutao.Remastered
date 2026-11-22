// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata;

/// <summary>
/// Selects where metadata comes from. <see cref="Auto"/> is a preference value only: it is never a
/// provider kind and never a DI key.
/// </summary>
public enum MetadataSourceKind
{
    /// <summary>
    /// Prefer the proto bundle, fall back to the git JSON tree silently.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Prefer the proto bundle; a fallback to git is reported once.
    /// </summary>
    Proto = 1,

    /// <summary>
    /// Only the git JSON tree.
    /// </summary>
    Git = 2,
}
