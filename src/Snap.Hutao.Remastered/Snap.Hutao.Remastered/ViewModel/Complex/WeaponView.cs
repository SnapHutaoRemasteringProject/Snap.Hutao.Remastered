// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;

namespace Snap.Hutao.Remastered.ViewModel.Complex;

internal sealed class WeaponView : CollocationView
{
    public WeaponView(Weapon weapon, double rate, double? lastRate)
        : base(rate, lastRate)
    {
        Name = weapon.Name;
        Icon = EquipIconConverter.IconNameToUri(weapon.Icon);
        Quality = weapon.Quality;
    }

    public override string Name { get; }

    public override Uri Icon { get; }

    public override QualityType Quality { get; }
}