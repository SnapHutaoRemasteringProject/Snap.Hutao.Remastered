// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Data;
using Snap.Hutao.Remastered.Service.UIGF;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

public sealed partial class UIGFVersionToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is UIGFVersion version)
        {
            return version switch
            {
                UIGFVersion.UIGF40 => "v4.0",
                UIGFVersion.UIGF41 => "v4.1",
                UIGFVersion.UIGF42 => "v4.2",
                _ => version.ToString(),
            };
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string str)
        {
            return str switch
            {
                "v4.0" => UIGFVersion.UIGF40,
                "v4.1" => UIGFVersion.UIGF41,
                "v4.2" => UIGFVersion.UIGF42,
                _ => UIGFVersion.None,
            };
        }

        return UIGFVersion.None;
    }
}
