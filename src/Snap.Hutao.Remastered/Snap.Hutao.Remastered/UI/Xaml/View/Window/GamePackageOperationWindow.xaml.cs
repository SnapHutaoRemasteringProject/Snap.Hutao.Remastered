// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.Graphics;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.Game.Package.Advanced;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.ViewModel.Game;
using System.Collections.Immutable;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

[Service(ServiceLifetime.Scoped)]
internal sealed partial class GamePackageOperationWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar,
    IXamlWindowClosedHandler
{
    private readonly TaskCompletionSource closeTcs = new();

    public GamePackageOperationWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        RectInt32 workArea = DisplayArea.Primary.WorkArea;
        SizeInt32 size = new(workArea.Height, (int)(workArea.Height * 0.75));
        AppWindow.Resize(size.Scale(0.5 * this.RasterizationScale));

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        IServiceScope scope = serviceProvider.CreateScope();
        this.InitializeController(scope.ServiceProvider);
        RootGrid.InitializeDataContext<GamePackageOperationViewModel>(scope.ServiceProvider);
    }

    public FrameworkElement TitleBarCaptionAccess { get => DraggableGrid; }

    public ImmutableArray<FrameworkElement> TitleBarPassthrough { get => []; }

    public Task CloseTask { get => closeTcs.Task; }

    public void OnWindowClosing(out bool cancel)
    {
        cancel = RootGrid.DataContext<GamePackageOperationViewModel>() is { IsFinished: false };
    }

    public void OnWindowClosed()
    {
        closeTcs.TrySetResult();
    }

    internal void HandleProgressUpdate(GamePackageOperationReport status)
    {
        RootGrid.DataContext<GamePackageOperationViewModel>()?.HandleProgressUpdate(status);
    }

    [Command("CloseCommand")]
    private void CloseWindow()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Close Window", "GamePackageOperationWindow.Command"));
        Close();
    }
}