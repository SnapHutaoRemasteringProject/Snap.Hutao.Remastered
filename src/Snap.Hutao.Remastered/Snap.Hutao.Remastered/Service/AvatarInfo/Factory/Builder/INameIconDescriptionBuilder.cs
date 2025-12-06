// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Abstraction;
using Snap.Hutao.Remastered.ViewModel.AvatarProperty;

namespace Snap.Hutao.Remastered.Service.AvatarInfo.Factory.Builder;

internal interface INameIconDescriptionBuilder<out T> : IBuilder
    where T : NameIconDescription
{
    T View { get; }
}