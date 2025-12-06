// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.AvatarProperty;

namespace Snap.Hutao.Remastered.Service.AvatarInfo.Factory.Builder;

internal sealed class WeaponViewBuilder : IWeaponViewBuilder
{
    public WeaponView View { get; } = new();
}