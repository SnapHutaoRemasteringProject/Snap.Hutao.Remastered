// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;

internal interface IMetadataArrayHyperLinkNameSource
{
    ImmutableArray<HyperLinkName> HyperLinkNames { get; set; }
}