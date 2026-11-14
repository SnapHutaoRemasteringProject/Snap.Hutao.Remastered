// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.


namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;
[DependencyProperty<double>("Maximum", NotNull = true)]
[DependencyProperty<double>("Value", NotNull = true)]
[DependencyProperty<bool>("IsIndeterminate", NotNull = true, DefaultValue = false)]
[DependencyProperty<string>("Header")]
[DependencyProperty<string>("Description")]
public sealed partial class CardProgressBar : Microsoft.UI.Xaml.Controls.Control
{
    public CardProgressBar()
    {
        DefaultStyleKey = typeof(CardProgressBar);
    }
}