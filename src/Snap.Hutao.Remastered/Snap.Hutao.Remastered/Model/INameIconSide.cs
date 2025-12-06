// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model;

internal interface INameIconSide<out TIcon> : INameIcon<TIcon>
{
    TIcon SideIcon { get; }
}