// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using System.Globalization;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter.Specialized;

internal sealed partial class ElementTypeIconConverter : ValueConverter<ElementType, Uri?>
{
    public override Uri? Convert(ElementType from)
    {
        string? name = from.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture);
        return string.IsNullOrEmpty(name) ? default : ElementNameIconConverter.ElementNameToUri(name);
    }
}