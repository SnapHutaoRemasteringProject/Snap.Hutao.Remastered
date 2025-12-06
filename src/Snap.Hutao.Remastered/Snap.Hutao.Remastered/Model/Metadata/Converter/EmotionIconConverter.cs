// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Data.Converter;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

internal sealed partial class EmotionIconConverter : ValueConverter<string, Uri>, IIconNameToUriConverter
{
    public static Uri IconNameToUri(string name)
    {
        return StaticResourcesEndpoints.StaticRaw("EmotionIcon", $"{name}.png").ToUri();
    }

    public override Uri Convert(string from)
    {
        return IconNameToUri(from);
    }
}