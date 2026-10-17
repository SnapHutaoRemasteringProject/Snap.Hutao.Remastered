// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using ABI.Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.UI.Xaml.Media.Backdrop;
using Snap.Hutao.Remastered.Win32.UI.Shell;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Windows.UI;
using WinRT;
using WinRT.Interop;
using AppWindowTitleBar = Microsoft.UI.Windowing.AppWindowTitleBar;

namespace Snap.Hutao.Remastered.UI.Windowing;

[SuppressMessage("", "CA1001")]
[SuppressMessage("", "SA1204")]
public sealed class XamlWindowController
{
    private readonly Type windowType;
    private readonly Window window;
    private readonly bool hasCustomSystemBackdrop;

    private readonly XamlWindowSubclass subclass;
    private readonly XamlWindowNonRude nonRude;
    private Task? detachContent;
    // Accepting a close request does not yet permit native island destruction.
    private bool closing;
    private bool readyToClose;

    public XamlWindowController(Window window, IServiceProvider serviceProvider)
    {
        windowType = window.GetType();
        this.window = window;
        Debug.Assert(serviceProvider is IServiceScope);
        ServiceProvider = serviceProvider;

        // Subclassing and NonRudeHWND are standard infrastructure.
        subclass = new(window);
        nonRude = new(window.GetWindowHandle());

        window.AppWindow.Title = SH.FormatAppNameAndVersion(HutaoRuntime.Version);
        window.AppWindow.SetIcon(InstalledLocation.GetAbsolutePath("Assets/Logo.ico"));

        // ExtendContentIntoTitleBar
        if (window is IXamlWindowExtendContentIntoTitleBar xamlWindow)
        {
            AppWindowTitleBar appTitleBar = window.AppWindow.TitleBar;
            appTitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            appTitleBar.ExtendsContentIntoTitleBar = true;

            UpdateTitleButtonColor(default!, default!);
            xamlWindow.TitleBarCaptionAccess.ActualThemeChanged += UpdateTitleButtonColor;

            if (xamlWindow.TitleBarCaptionAccess is not TitleBar)
            {
                XamlWindowRegionRects.Update(window);
                xamlWindow.TitleBarCaptionAccess.SizeChanged += OnWindowSizeChanged;
            }
        }

        // Size stuff
        if (window is IXamlWindowHasInitSize xamlWindow2)
        {
            window.AppWindow.Resize(xamlWindow2.InitSize);
        }

        // window.AppWindow.EnablePlacementPersistence(guid, window is MainWindow, default, PlacementPersistenceBehaviorFlags.Default, windowName);
        EnablePlacementRestoration(window);

        window.Content.As<FrameworkElement>().Loading += OnWindowContentLoading;

        window.AppWindow.Show(true);
        window.AppWindow.MoveInZOrderAtTop();

        hasCustomSystemBackdrop = window.SystemBackdrop is not null;

        // SystemBackdrop
        AppOptions appOptions = serviceProvider.GetRequiredService<AppOptions>();
        UpdateSystemBackdrop(appOptions.BackdropType.Value);
        BackdropTypeCallback = appOptions.BackdropType.WithValueChangedCallback(static (value, controller) => controller.UpdateSystemBackdrop(value), this);

        subclass.Initialize();
        window.Closed += OnWindowClosed;
    }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IServiceProvider ServiceProvider { get; }

    private IObservableProperty<BackdropType>? BackdropTypeCallback { get; }

    public Task DetachContentAsync()
    {
        if (detachContent is not null) return detachContent;
        TaskCompletionSource detached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        detachContent = detached.Task;
        if (window.Content is FrameworkElement root && root.IsLoaded)
        {
            root.Loading -= OnWindowContentLoading;
            RoutedEventHandler? unloaded = null;
            unloaded = (_, _) =>
            {
                root.Unloaded -= unloaded;
                // Parent Unloaded may precede descendant Unloaded. Drain the
                // entire event cascade before disposing scopes or native players.
                window.DispatcherQueue.TryEnqueue(() => detached.TrySetResult());
            };
            root.Unloaded += unloaded;
            window.Content = null;
        }
        else
        {
            window.Content = null;
            detached.TrySetResult();
        }

        window.SystemBackdrop = null;
        return detachContent;
    }

    private async Task CloseAfterDetachAsync()
    {
        await DetachContentAsync().ConfigureAwait(true);
        readyToClose = true;
        // Do not recursively close from inside the first native Closed callback.
        window.DispatcherQueue.TryEnqueue(window.Close);
    }

    public bool TrySetTaskbarProgress(TBPFLAG state, ulong value, ulong maximum)
    {
        try
        {
            subclass.SetTaskbarProgress(state, value, maximum);
            return true;
        }
        catch (Exception ex)
        {
            Debugger.Break();
            SentrySdk.CaptureException(ex);
            return false;
        }
    }

    private static void EnablePlacementRestoration(Window window)
    {
        IObjectReference objRefAppWindowExperimental = Unsafe.As<IWinRTObject>(window.AppWindow).NativeObject.As<IUnknownVftbl>(IAppWindowExperimentalMethods.IID);
        IAppWindowExperimentalMethods.set_PlacementRestorationBehavior(objRefAppWindowExperimental, PlacementRestorationBehavior.All);

        string windowName = TypeNameHelper.GetTypeDisplayName(window);
        byte[] data = CryptographicOperations.HashData(HashAlgorithmName.MD5, Encoding.UTF8.GetBytes(windowName));
        Guid guid = MemoryMarshal.AsRef<Guid>(data);
        IAppWindowExperimentalMethods.set_PersistedStateId(objRefAppWindowExperimental, guid);
    }

    private void OnWindowContentLoading(FrameworkElement element, object e)
    {
        element.Loading -= OnWindowContentLoading;
        element.XamlRoot.ContentIsland.AppData = new XamlContext
        {
            ServiceProvider = ServiceProvider,
        };
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateDebug("WindowClosing", "XamlWindowController", [("type", TypeNameHelper.GetTypeDisplayName(window, false))]));

        if (args.Handled)
        {
            return;
        }

        if (closing && !readyToClose)
        {
            // Repeated close requests must not bypass the pending unload phase.
            args.Handled = true;
            return;
        }

        if (!closing && window is IXamlWindowClosedHandler handler)
        {
            handler.OnWindowClosing(out bool cancel);
            if (cancel)
            {
                args.Handled = true;
                return;
            }
        }

        if (!closing && !XamlApplicationLifetime.Exiting)
        {
            IServiceProviderIsKeyedService isKeyedService = ServiceProvider.GetRequiredService<IServiceProviderIsKeyedService>();
            ICurrentXamlWindowReference currentXamlWindowReference = isKeyedService.IsKeyedService(typeof(ICurrentXamlWindowReference), windowType)
                ? ServiceProvider.GetRequiredKeyedService<ICurrentXamlWindowReference>(windowType)
                : ServiceProvider.GetRequiredService<ICurrentXamlWindowReference>();

            if (currentXamlWindowReference.Window == window)
            {
                // Only a CurrentWindow can show dialogs
                // Some users might try to close the window while a dialog is showing
                if (ServiceProvider.GetRequiredService<IContentDialogFactory>().IsDialogShowing)
                {
                    args.Handled = true;
                    return;
                }

                currentXamlWindowReference.Window = default!;
            }
        }

        if (!closing)
        {
            // Closed is cancelable. Keep the native island/dispatcher alive until
            // XAML descendants have detached their behaviors and async callbacks.
            closing = true;
            args.Handled = true;
            CloseAfterDetachAsync().SafeForget();
            return;
        }

        // Detach events
        window.Closed -= OnWindowClosed;
        if (window is IXamlWindowExtendContentIntoTitleBar xamlWindow)
        {
            xamlWindow.TitleBarCaptionAccess.ActualThemeChanged -= UpdateTitleButtonColor;
            xamlWindow.TitleBarCaptionAccess.SizeChanged -= OnWindowSizeChanged;
        }

        // Dispose components
        subclass.Dispose();
        nonRude.Dispose();

        (window as IXamlWindowClosedHandler)?.OnWindowClosed();

        // Dispose the service scope
        Unsafe.As<IServiceScope>(ServiceProvider).Dispose();
        window.UninitializeController();
    }

    private bool UpdateSystemBackdrop(BackdropType backdropType)
    {
        if (closing || detachContent is not null) return false;
        if (hasCustomSystemBackdrop)
        {
            return true;
        }

        window.SystemBackdrop = backdropType switch
        {
            BackdropType.Transparent => new TransparentBackdrop(),
            BackdropType.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
            BackdropType.Mica => new MicaBackdrop { Kind = MicaKind.Base },
            BackdropType.Acrylic => new DesktopAcrylicBackdrop(),
            _ => null,
        };

        return true;
    }

    private void UpdateTitleButtonColor(FrameworkElement discardElement, object e)
    {
        if (window is not IXamlWindowExtendContentIntoTitleBar xamlWindow)
        {
            return;
        }

        AppWindowTitleBar appTitleBar = window.AppWindow.TitleBar;

        appTitleBar.ButtonBackgroundColor = Colors.Transparent;
        appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        bool isDarkMode = ThemeHelper.IsDarkMode(xamlWindow.TitleBarCaptionAccess.ActualTheme);

        Color systemBaseLowColor = SystemColors.BaseLowColor(isDarkMode);
        appTitleBar.ButtonHoverBackgroundColor = systemBaseLowColor;

        Color systemBaseMediumLowColor = SystemColors.BaseMediumLowColor(isDarkMode);
        appTitleBar.ButtonPressedBackgroundColor = systemBaseMediumLowColor;

        // The Foreground doesn't accept Alpha channel. So we translate it to gray.
        byte light = (byte)((systemBaseMediumLowColor.R + systemBaseMediumLowColor.G + systemBaseMediumLowColor.B) / 3);
        byte result = (byte)(systemBaseMediumLowColor.A / 255.0 * light);
        appTitleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, result, result, result);

        Color systemBaseHighColor = SystemColors.BaseHighColor(isDarkMode);
        appTitleBar.ButtonForegroundColor = systemBaseHighColor;
        appTitleBar.ButtonHoverForegroundColor = systemBaseHighColor;
        appTitleBar.ButtonPressedForegroundColor = systemBaseHighColor;
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        XamlWindowRegionRects.Update(window);
    }
}
