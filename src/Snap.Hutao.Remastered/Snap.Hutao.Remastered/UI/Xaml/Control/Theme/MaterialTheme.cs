// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using WinUI.Composition.Hlsl;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>Material style is independent of the application's light/dark theme and window backdrop.</summary>
public static class MaterialTheme
{
    private static readonly List<WeakReference<ResourceDictionary>> RegisteredDictionaries = [];
    private static ControlMaterial currentMaterial;

    public static void Register(ResourceDictionary dictionary)
    {
        RegisteredDictionaries.Add(new(dictionary));
        Apply(dictionary, currentMaterial);
    }

    public static void Apply(ControlMaterial material)
    {
        currentMaterial = material;

        if (Application.Current is not { } application)
        {
            return;
        }

        foreach (ResourceDictionary dictionary in application.Resources.MergedDictionaries)
        {
            // Application resources outlive every Frame page. Keep their composition
            // brushes on fallback so a page detach cannot reconnect a process-wide
            // brush while NavigationView is arranging its replacement content.
            Apply(dictionary, ControlMaterial.Default);
        }

        for (int index = RegisteredDictionaries.Count - 1; index >= 0; index--)
        {
            if (RegisteredDictionaries[index].TryGetTarget(out ResourceDictionary? dictionary))
            {
                Apply(dictionary, material);
            }
            else
            {
                RegisteredDictionaries.RemoveAt(index);
            }
        }
    }

    private static void Apply(ResourceDictionary dictionary, ControlMaterial material)
    {
        foreach (object value in dictionary.ThemeDictionaries.Values)
        {
            if (value is not ResourceDictionary variant)
            {
                continue;
            }

            foreach (string key in new[] { "HutaoTitleCardBrush", "HutaoCardBrush", "HutaoSurfaceBrush", "HutaoGuideLiquidGlassBrush" })
            {
                if (variant.TryGetValue(key, out object? resource) && resource is LiquidGlassBrush brush)
                {
                    brush.IsEnabled = material is ControlMaterial.LiquidGlass;
                }
            }
        }
    }
}
