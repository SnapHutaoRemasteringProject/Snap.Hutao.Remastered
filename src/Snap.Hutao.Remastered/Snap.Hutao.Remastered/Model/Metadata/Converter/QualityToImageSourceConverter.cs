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
    public QualityToImageSourceConverter()
    {
        RedSource = CreateSource("UI_QUALITY_RED.png");
        OrangeSource = CreateSource("UI_QUALITY_ORANGE.png");
        PurpleSource = CreateSource("UI_QUALITY_PURPLE.png");
        BlueSource = CreateSource("UI_QUALITY_BLUE.png");
        GreenSource = CreateSource("UI_QUALITY_GREEN.png");
        WhiteSource = CreateSource("UI_QUALITY_WHITE.png");
        NoneSource = CreateSource("UI_QUALITY_NONE.png");
    }

    /// <summary>
    /// Gets the instance used by template bindings that cannot resolve a resource dictionary entry.
    /// </summary>
    public static QualityToImageSourceConverter Default { get; } = new();

    public override ImageSource? Convert(QualityType from)
    {
        return from switch
        {
            QualityType.QUALITY_ORANGE_SP => RedSource,
            QualityType.QUALITY_ORANGE => OrangeSource,
            QualityType.QUALITY_PURPLE => PurpleSource,
            QualityType.QUALITY_BLUE => BlueSource,
            QualityType.QUALITY_GREEN => GreenSource,
            QualityType.QUALITY_WHITE => WhiteSource,
            _ => NoneSource,
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
