// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

public sealed partial class ItemFlyoutContentResources : ResourceDictionary
{
    public ItemFlyoutContentResources()
    {
        InitializeComponent();
    }

    // A ControlTemplate x:Bind never receives its converter lookup root, so a
    // {StaticResource} converter throws in the generated LookupConverter. Call the
    // mapping directly instead, the same way AutoSortListResources.CreateIcon does.
    public static Visibility VisibleIf(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }
}
