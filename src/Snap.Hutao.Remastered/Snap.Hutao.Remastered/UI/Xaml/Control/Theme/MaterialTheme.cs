// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using WinUI.Composition.Hlsl;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>Material style is independent of the application's light/dark theme and window backdrop.</summary>
public static class MaterialTheme
{
    public static void Apply(ControlMaterial material)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        foreach (ResourceDictionary dictionary in application.Resources.MergedDictionaries)
        {
            foreach (object value in dictionary.ThemeDictionaries.Values)
            {
                if (value is not ResourceDictionary variant)
                {
                    continue;
                }

                foreach (string key in new[] { "HutaoTitleCardBrush", "HutaoCardBrush", "HutaoSurfaceBrush" })
                {
                    if (variant.TryGetValue(key, out object? resource) && resource is LiquidGlassBrush brush)
                    {
                        brush.IsEnabled = material is ControlMaterial.LiquidGlass;
                    }
                }
            }
        }
    }
}
