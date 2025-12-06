// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.DailyNote;

internal class DailyNoteMetadataContext : IMetadataContext,
    IMetadataArrayChapterSource
{
    public ImmutableArray<Chapter> Chapters { get; set; } = default!;
}
