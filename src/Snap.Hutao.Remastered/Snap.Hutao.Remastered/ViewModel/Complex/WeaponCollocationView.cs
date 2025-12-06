// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Complex;

internal sealed class WeaponCollocationView
{
    public required ImmutableArray<AvatarView> Avatars { get; init; }

    internal required WeaponId WeaponId { get; init; }
}