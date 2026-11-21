// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.Win32.Foundation;

namespace Snap.Hutao.Remastered.Core.LifeCycle;

public static class CurrentXamlWindowReferenceExtension
{
    extension(ICurrentXamlWindowReference reference)
    {
        public XamlRoot? XamlRoot { get => reference.Window?.Content?.XamlRoot; }

        public HWND WindowHandle { get => reference.Window?.GetWindowHandle() ?? default; }

        public ElementTheme RequestedTheme
        {
            [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.FrameworkElement))]
            get
            {
                ArgumentNullException.ThrowIfNull(reference.Window);
                return ((FrameworkElement)reference.Window.Content).RequestedTheme;
            }
        }
    }
}