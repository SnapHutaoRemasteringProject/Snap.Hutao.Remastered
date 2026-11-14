// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSortBox;

public sealed partial class AutoSortListResources : ResourceDictionary
{
    public AutoSortListResources()
    {
        InitializeComponent();
    }

    public static BitmapImage? CreateIcon(Uri? uri) => uri is null ? null : new BitmapImage(uri);
}
