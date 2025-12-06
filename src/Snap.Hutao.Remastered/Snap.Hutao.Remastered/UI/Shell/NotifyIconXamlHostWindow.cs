// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Snap.Hutao.Remastered.Core.Graphics;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml.Media.Backdrop;
using Snap.Hutao.Remastered.Win32.Foundation;
using Windows.Foundation;

namespace Snap.Hutao.Remastered.UI.Shell;

internal sealed class NotifyIconXamlHostWindow : Window, IWindowNeedEraseBackground, IXamlWindowClosedHandler
{
    public NotifyIconXamlHostWindow(IServiceProvider serviceProvider)
    {
        Content = new Border();

        this.AddExtendedStyleLayered();
        this.SetLayeredWindowTransparency(0);
        this.AddExtendedStyleToolWindow();

        AppWindow.Title = "SnapHutaoNotifyIconXamlHost";
        AppWindow.SafeIsShowInSwitchers(false);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(false, false);
        }

        IServiceScope scope = serviceProvider.CreateScope();
        this.InitializeController(scope.ServiceProvider);
    }

    public void ShowFlyoutAt(FlyoutBase flyout, Point point, RECT icon)
    {
        icon.left -= 8;
        icon.top -= 8;
        icon.right += 8;
        icon.bottom += 8;

        if (AppWindow is null || Content?.XamlRoot is null /*ERROR_XAMLROOT_REQUIRED*/)
        {
            return;
        }

        if (flyout.IsDisposed)
        {
            return;
        }

        this.SwitchTo();
        MoveAndResize(icon);

        flyout.ShowAt(Content, new()
        {
            Placement = FlyoutPlacementMode.Auto,
            ShowMode = FlyoutShowMode.Standard,
        });
    }

    public void MoveAndResize(RECT icon)
    {
        AppWindow.MoveAndResize(RectInt32Convert.RectInt32(icon));
    }

    public void OnWindowClosing(out bool cancel)
    {
        // https://github.com/DGP-Studio/Snap.Hutao.Remastered/issues/2532
        // Prevent the window closing when the application is not exiting.
        cancel = !XamlApplicationLifetime.Exiting;
    }

    public void OnWindowClosed()
    {
    }
}