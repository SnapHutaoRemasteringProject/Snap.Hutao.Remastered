// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.


namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

[DependencyProperty<string>("Text")]
[DependencyProperty<object>("ImageSource")]
[DependencyProperty<double>("IconSquareLength", NotNull = true)]
public sealed partial class CardBlock : Microsoft.UI.Xaml.Controls.Control
{
    public CardBlock()
    {
        DefaultStyleKey = typeof(CardBlock);
    }
}