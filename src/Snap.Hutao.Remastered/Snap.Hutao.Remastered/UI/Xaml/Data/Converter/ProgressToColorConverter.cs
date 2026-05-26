// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

/// <summary>
/// Converts a progress value (0-1) to a color. Returns green when progress is 1 (100%), otherwise returns accent color.
/// </summary>
public sealed partial class ProgressToColorConverter : ValueConverter<double, Brush>
{
    public override Brush Convert(double from)
    {
        if (from >= 1.0)
            return new SolidColorBrush(Colors.Green);

        return Application.Current.Resources["ProgressBarForeground"] as Brush;
    }
}