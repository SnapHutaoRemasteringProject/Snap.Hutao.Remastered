// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class BackpackReliquarySubStatView : ObservableObject
{
    public static readonly BackpackReliquarySubStatView Empty = new() { HasValue = false };

    public FightProperty FightProp { get; init => SetProperty(ref field, value); }

    public float Value { get; init => SetProperty(ref field, value); }

    public uint EnhancedCount { get; init => SetProperty(ref field, value); }

    public bool HasValue { get; init => SetProperty(ref field, value); } = true;

    public ReliquarySubStatState State { get; init => SetProperty(ref field, value); } = ReliquarySubStatState.Effective;

    public string DisplayName => HasValue ? GetDisplayName() : string.Empty;

    public string DisplayValue => HasValue
        ? (FightProp.IsFightPropPercent()
            ? Value.ToString("P1", CultureInfo.CurrentCulture)
            : Value.ToString("F0", CultureInfo.CurrentCulture))
        : string.Empty;

    private string GetDisplayName()
    {
        string name = FightProp.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!;
        return State is ReliquarySubStatState.Inactive
            ? $"{name} {SH.ViewPageBackpackReliquarySubStatInactive}"
            : name;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(HasValue):
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayValue));
                break;
            case nameof(FightProp):
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayValue));
                break;
            case nameof(Value):
                OnPropertyChanged(nameof(DisplayValue));
                break;
            case nameof(State):
                OnPropertyChanged(nameof(DisplayName));
                break;
        }
    }
}
