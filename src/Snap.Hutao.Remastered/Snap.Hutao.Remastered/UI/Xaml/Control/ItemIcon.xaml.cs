// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Converter;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

public sealed partial class ItemIconResources : ResourceDictionary
{
    public ItemIconResources()
    {
        InitializeComponent();
    }

    // A ControlTemplate x:Bind never receives its converter lookup root, so a
    // {StaticResource} converter throws in the generated LookupConverter. Call the
    // mapping directly instead, the same way AutoSortListResources.CreateIcon does.
    public static ImageSource? CreateQualityImage(QualityType quality)
    {
        return QualityToImageSourceConverter.Default.Convert(quality);
    }
}
