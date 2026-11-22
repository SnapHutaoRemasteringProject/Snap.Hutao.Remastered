// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Signals that a single entry cannot be represented in the application model. The adapter drops that
/// entry and keeps the rest of the category; any other exception fails the whole category.
/// </summary>
internal sealed class MetadataEntrySkippedException : Exception
{
    public MetadataEntrySkippedException(string message)
        : this(string.Empty, message)
    {
    }

    public MetadataEntrySkippedException(string field, string message)
        : base(message)
    {
        Field = field;
    }

    /// <summary>
    /// The proto field that could not be converted; used to report each field once per category.
    /// </summary>
    public string Field { get; }
}
