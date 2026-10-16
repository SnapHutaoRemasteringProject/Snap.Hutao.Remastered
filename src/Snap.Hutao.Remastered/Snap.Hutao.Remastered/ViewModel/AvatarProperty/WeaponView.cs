// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Calculable;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Model;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

public sealed partial class WeaponView : EquipView, ICalculableSource<ICalculableWeapon>
{
    [ObservableProperty]
    public partial NameValue<string>? SubProperty { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AffixLevel))]
    public partial uint AffixLevelNumber { get; set; }

    public string AffixLevel { get => SH.FormatModelBindingAvatarPropertyWeaponAffix(AffixLevelNumber); }

    [ObservableProperty]
    public partial string AffixName { get; set; } = default!;

    [ObservableProperty]
    public partial string AffixDescription { get; set; } = default!;

    [ObservableProperty]
    public partial ImmutableArray<bool> PromoteArray { get; set; } = [];

    public WeaponId Id { get; set => SetProperty(ref field, value); }

    [ObservableProperty]
    public partial uint LevelNumber { get; set; }

    public uint MaxLevel { get => Model.Metadata.Weapon.Weapon.GetMaxLevelByQuality(Quality); }

    [ObservableProperty]
    public partial WeaponType WeaponType { get; set; }

    public PromoteLevel PromoteLevel { get; set => SetProperty(ref field, value); }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Quality)) OnPropertyChanged(nameof(MaxLevel));
    }

    public ICalculableWeapon ToCalculable()
    {
        return CalculableWeapon.From(this);
    }
}
