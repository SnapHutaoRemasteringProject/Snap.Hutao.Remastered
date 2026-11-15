// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;
public sealed partial class QualityToImageSourceConverter : DependencyValueConverter<QualityType, ImageSource?>
{
    private readonly ImageSource redSource = CreateSource("UI_QUALITY_RED.png");
    private readonly ImageSource orangeSource = CreateSource("UI_QUALITY_ORANGE.png");
    private readonly ImageSource purpleSource = CreateSource("UI_QUALITY_PURPLE.png");
    private readonly ImageSource blueSource = CreateSource("UI_QUALITY_BLUE.png");
    private readonly ImageSource greenSource = CreateSource("UI_QUALITY_GREEN.png");
    private readonly ImageSource whiteSource = CreateSource("UI_QUALITY_WHITE.png");
    private readonly ImageSource noneSource = CreateSource("UI_QUALITY_NONE.png");

    /// <summary>
    /// Gets the instance used by template bindings that cannot resolve a resource dictionary entry.
    /// </summary>
    public static QualityToImageSourceConverter Default { get; } = new();

    public override ImageSource? Convert(QualityType from)
    {
        return from switch
        {
            QualityType.QUALITY_ORANGE_SP => redSource,
            QualityType.QUALITY_ORANGE => orangeSource,
            QualityType.QUALITY_PURPLE => purpleSource,
            QualityType.QUALITY_BLUE => blueSource,
            QualityType.QUALITY_GREEN => greenSource,
            QualityType.QUALITY_WHITE => whiteSource,
            _ => noneSource,
        };
    }

    private static BitmapImage CreateSource(string fileName)
    {
        // Template x:Bind cannot reach a converter resource of its own dictionary,
        // so the images live here. Unpackaged runs cannot resolve ms-appx:///.
        Uri msAppxUri = new($"ms-appx:///Resource/ItemIcon/{fileName}");
        return new BitmapImage(RuntimeEnvironment.IsPackaged ? msAppxUri : InstalledLocation.ToAbsoluteUri(msAppxUri));
    }
}
