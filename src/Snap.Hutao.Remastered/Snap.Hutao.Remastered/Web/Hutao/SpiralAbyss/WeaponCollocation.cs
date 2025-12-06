// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hutao.SpiralAbyss;

internal sealed class WeaponCollocation : WeaponBuild
{
    public List<ItemRate<AvatarId, double>> Avatars { get; set; } = default!;
}