// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Setting;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

internal sealed class CardReference
{
    public Button? Card { get; set; }

    public int Order { get; set; }

    public static CardReference Create(Button card, string order)
    {
        return new() { Card = card, Order = LocalSetting.Get(order, 0) };
    }
}