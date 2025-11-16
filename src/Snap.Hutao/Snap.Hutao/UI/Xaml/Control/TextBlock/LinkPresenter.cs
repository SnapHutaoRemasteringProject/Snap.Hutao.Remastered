// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.ViewModel.Wiki;

namespace Snap.Hutao.UI.Xaml.Control.TextBlock;

[DependencyProperty<string>("LinkName")]
[DependencyProperty<string>("LinkDescription")]
[DependencyProperty<LinkMetadataContext>("LinkContext")]
internal sealed partial class LinkPresenter : ContentControl
{
    public LinkPresenter()
    {
        DefaultStyleKey = typeof(LinkPresenter);
    }
}