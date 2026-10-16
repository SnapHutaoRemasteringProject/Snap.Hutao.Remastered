// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.Setting;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

public sealed partial class CardReference : ObservableObject
{
    [ObservableProperty]
    public partial FrameworkElement? Card { get; set; }

    [ObservableProperty]
    public partial int Order { get; set; }

    public static CardReference Create(FrameworkElement card, string order)
    {
        return new() { Card = card, Order = LocalSetting.Get(order, 0) };
    }
}
