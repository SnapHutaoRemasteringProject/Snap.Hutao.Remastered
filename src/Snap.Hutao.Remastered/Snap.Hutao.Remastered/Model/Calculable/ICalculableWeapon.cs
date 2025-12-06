// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Calculable;

internal interface ICalculableWeapon : ICalculablePromoteLevel
{
    WeaponId WeaponId { get; }
}