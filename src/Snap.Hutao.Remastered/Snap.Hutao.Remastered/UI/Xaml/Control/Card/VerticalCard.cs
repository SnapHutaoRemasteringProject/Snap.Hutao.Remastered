// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;
[DependencyProperty<Thickness>("BottomPadding", NotNull = true)]
public sealed partial class VerticalCard : Microsoft.UI.Xaml.Controls.Control
{
    public VerticalCard()
    {
        DefaultStyleKey = typeof(VerticalCard);
    }
}