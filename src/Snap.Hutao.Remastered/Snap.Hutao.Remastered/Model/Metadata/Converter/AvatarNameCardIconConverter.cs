// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Data.Converter;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

public sealed partial class AvatarNameCardIconConverter : ValueConverter<string, Uri>, IIconNameToUriConverter
{
    public static Uri IconNameToUri(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return default!;
        }

        string category = name.StartsWith("UI_NameCardPic_", StringComparison.Ordinal)
            ? "NameCardPic"
            : "NameCardIcon";

        return StaticResourcesEndpoints.StaticRaw(category, $"{name}.png").ToUri();
    }

    public override Uri Convert(string from)
    {
        return IconNameToUri(from);
    }
}
