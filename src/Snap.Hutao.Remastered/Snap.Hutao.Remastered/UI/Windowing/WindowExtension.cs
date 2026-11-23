// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.UI.Shell;
using Snap.Hutao.Remastered.Win32.Foundation;
using Snap.Hutao.Remastered.Win32.UI.Shell;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using WinRT.Interop;

namespace Snap.Hutao.Remastered.UI.Windowing;

public static class WindowExtension
{
    private static readonly ConditionalWeakTable<Window, XamlWindowController> WindowControllers = [];
    private static DevWinUI.EcoQosProcessPriority? foregroundPriority;
    private static bool hasTrackedWindow;

    public static Task DetachAllContentAsync()
    {
        // Application.Exit already accepts process-wide shutdown. Do not route it
        // through per-window close vetoes (e.g. an active download or WebView load).
        return Task.WhenAll(WindowControllers.ToArray().Select(pair => pair.Value.DetachContentAsync()));
    }

    extension<TWindow>(TWindow window)
        where TWindow : Window
    {
        public void InitializeController(IServiceProvider serviceProvider)
        {
            XamlWindowController windowController = new(window, serviceProvider);
            WindowControllers.Add(window, windowController);
            if (window is not NotifyIconXamlHostWindow)
            {
                hasTrackedWindow = true;
                window.AppWindow.Changed += OnEfficiencyWindowChanged;
                UpdateEfficiencyMode();
            }
        }

        public void UninitializeController()
        {
            if (WindowControllers.Remove(window) && window is not NotifyIconXamlHostWindow)
            {
                if (window.AppWindow is { } appWindow)
                {
                    appWindow.Changed -= OnEfficiencyWindowChanged;
                }

                UpdateEfficiencyMode();
            }
        }

        public bool TrySetTaskbarProgress(TBPFLAG state, ulong value, ulong maximum)
        {
            if (!WindowControllers.TryGetValue(window, out XamlWindowController? controller))
            {
                return false;
            }

            return controller.TrySetTaskbarProgress(state, value, maximum);
        }
    }

    public static void UpdateEfficiencyMode()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        bool shouldEnable = hasTrackedWindow
            && XamlApplicationLifetime.NotifyIconCreated
            && !XamlApplicationLifetime.Exiting
            && !XamlApplicationLifetime.WindowClosedForGameLaunch
            && !WindowControllers.Any(static pair => IsForegroundWindow(pair.Key));
        if (shouldEnable == foregroundPriority.HasValue)
        {
            return;
        }

        try
        {
            if (shouldEnable)
            {
                using Process process = Process.GetCurrentProcess();
                DevWinUI.EcoQosProcessPriority priority = (DevWinUI.EcoQosProcessPriority)process.PriorityClass;
                DevWinUI.EcoQoSHelper.EnableEfficiencyMode();
                foregroundPriority = priority;
            }
            else if (foregroundPriority is { } priority)
            {
                DevWinUI.EcoQoSHelper.DisableEfficiencyMode(priority);
                foregroundPriority = null;
            }
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Windowing.OverlappedPresenter))]
    private static bool IsForegroundWindow(Window window)
    {
        return window is not NotifyIconXamlHostWindow
            && window.AppWindow is { IsVisible: true, Presenter: not OverlappedPresenter { State: OverlappedPresenterState.Minimized } };
    }

    private static void OnEfficiencyWindowChanged(AppWindow sender, AppWindowChangedEventArgs args) => UpdateEfficiencyMode();

    extension(Window window)
    {
        public double RasterizationScale
        {
            get
            {
                return window is { Content.XamlRoot: { } xamlRoot }
                    ? xamlRoot.RasterizationScale
                    : WindowUtilities.GetRasterizationScaleForWindow(window.GetWindowHandle());
            }
        }

        public bool TryGetAssociatedServiceProvider(out IServiceProvider serviceProvider)
        {
            if (WindowControllers.TryGetValue(window, out XamlWindowController? controller))
            {
                serviceProvider = controller.ServiceProvider;
                return true;
            }

            serviceProvider = default!;
            return false;
        }

        public HWND GetWindowHandle()
        {
            return WindowNative.GetWindowHandle(window);
        }

        public void SwitchTo()
        {
            WindowUtilities.SwitchToWindow(window.GetWindowHandle());
        }

        public void AddExtendedStyleLayered()
        {
            WindowUtilities.AddExtendedStyleLayered(window.GetWindowHandle());
        }

        public void RemoveExtendedStyleLayered()
        {
            WindowUtilities.RemoveExtendedStyleLayered(window.GetWindowHandle());
        }

        public void SetLayeredWindowTransparency(byte alpha)
        {
            WindowUtilities.SetLayeredWindowTransparency(window.GetWindowHandle(), alpha);
        }

        public void AddExtendedStyleToolWindow()
        {
            WindowUtilities.AddExtendedStyleToolWindow(window.GetWindowHandle());
        }

        public void RemoveStyleOverlappedWindow()
        {
            WindowUtilities.RemoveStyleOverlappedWindow(window.GetWindowHandle());
        }
    }
}
