// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

internal sealed class RoleCombatMetadataContext : IMetadataContext,
    IMetadataDictionaryIdRoleCombatScheduleSource,
    IMetadataDictionaryIdAvatarWithPlayersSource
{
    public ImmutableDictionary<RoleCombatScheduleId, RoleCombatSchedule> IdRoleCombatScheduleMap { get; set; } = default!;

    public ImmutableDictionary<AvatarId, Avatar> IdAvatarMap { get; set; } = default!;
}