// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.ExceptionService;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public static class ThemeHelper
{
    public static SystemBackdropTheme ElementToSystemBackdrop(ElementTheme elementTheme)
    {
        return elementTheme switch
        {
            ElementTheme.Default => SystemBackdropTheme.Default,
            ElementTheme.Light => SystemBackdropTheme.Light,
            ElementTheme.Dark => SystemBackdropTheme.Dark,
            _ => throw HutaoException.NotSupported($"Unexpected ElementTheme value: {elementTheme}."),
        };
    }

    public static ApplicationTheme ElementToApplication(ElementTheme applicationTheme)
    {
        return applicationTheme switch
        {
            ElementTheme.Light => ApplicationTheme.Light,
            ElementTheme.Dark => ApplicationTheme.Dark,
            _ => Application.Current.RequestedTheme,
        };
    }

    public static ElementTheme ApplicationToElement(ApplicationTheme applicationTheme)
    {
        return applicationTheme switch
        {
            ApplicationTheme.Light => ElementTheme.Light,
            ApplicationTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    public static bool IsDarkMode(ApplicationTheme applicationTheme)
    {
        return applicationTheme is ApplicationTheme.Dark;
    }

    public static bool IsDarkMode(ElementTheme elementTheme)
    {
        ApplicationTheme appTheme = Application.Current.RequestedTheme;
        return IsDarkMode(elementTheme, appTheme);
    }

    public static bool IsDarkMode(ElementTheme elementTheme, ApplicationTheme applicationTheme)
    {
        return elementTheme switch
        {
            ElementTheme.Default => IsDarkMode(applicationTheme),
            ElementTheme.Dark => true,
            _ => false,
        };
    }
}
