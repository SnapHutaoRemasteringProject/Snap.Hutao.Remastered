// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;

internal interface IMetadataDictionaryExtendedEquipAffixIdReliquarySetSource
{
    ImmutableDictionary<ExtendedEquipAffixId, ReliquarySet> ExtendedIdReliquarySetMap { get; set; }
}