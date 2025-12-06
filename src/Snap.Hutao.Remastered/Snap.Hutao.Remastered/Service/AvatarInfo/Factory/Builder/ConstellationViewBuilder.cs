// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.AvatarProperty;

namespace Snap.Hutao.Remastered.Service.AvatarInfo.Factory.Builder;

internal sealed class ConstellationViewBuilder : IConstellationViewBuilder
{
    public ConstellationView View { get; } = new();
}