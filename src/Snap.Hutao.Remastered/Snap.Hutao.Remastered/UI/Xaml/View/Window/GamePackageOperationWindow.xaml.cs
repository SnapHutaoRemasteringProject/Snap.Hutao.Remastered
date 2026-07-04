// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
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
using System.Diagnostics;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

[Service(ServiceLifetime.Scoped)]
public sealed partial class GamePackageOperationWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar,
    IXamlWindowClosedHandler
{
    private static readonly TimeSpan SpeedGraphUpdateInterval = TimeSpan.FromMilliseconds(200);

    private readonly TaskCompletionSource closeTcs = new();
    private ulong downloadMaxSpeed = 1;
    private ulong installMaxSpeed = 1;
    private long downloadSpeedGraphLastUpdateTimestamp;
    private long installSpeedGraphLastUpdateTimestamp;

    public GamePackageOperationWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        RectInt32 workArea = DisplayArea.Primary.WorkArea;
        SizeInt32 size = new(workArea.Height, (int)(workArea.Height * 0.75));
        AppWindow.Resize(size.Scale(0.5 * this.RasterizationScale));

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
#if DEBUG
            presenter.IsResizable = true;
#else
            presenter.IsResizable = false;
#endif
            presenter.IsMaximizable = false;
        }

        IServiceScope scope = serviceProvider.CreateScope();
        this.InitializeController(scope.ServiceProvider);
        RootGrid.InitializeDataContext<GamePackageOperationViewModel>(scope.ServiceProvider);
    }

    public FrameworkElement TitleBarCaptionAccess { get => DraggableGrid; }

    public ImmutableArray<FrameworkElement> TitleBarPassthrough { get => []; }

    public Task CloseTask { get => closeTcs.Task; }

    public void SetOperationContext(GamePackageOperationContext context)
    {
        RootGrid.DataContext<GamePackageOperationViewModel>()?.SetOperationContext(context);
    }

    public void OnWindowClosing(out bool cancel)
    {
        cancel = RootGrid.DataContext<GamePackageOperationViewModel>() is not { CanClose: true };
    }

    public void OnWindowClosed()
    {
        closeTcs.TrySetResult();
    }

    public void HandleProgressUpdate(GamePackageOperationReport status)
    {
        GamePackageOperationViewModel? viewModel = RootGrid.DataContext<GamePackageOperationViewModel>();
        viewModel?.HandleProgressUpdate(status);
        UpdateSpeedGraphs(viewModel, status);
    }

    [Command("CloseCommand")]
    private void CloseWindow()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Close Window", "GamePackageOperationWindow.Command"));
        Close();
    }

    private void UpdateSpeedGraphs(GamePackageOperationViewModel? viewModel, GamePackageOperationReport status)
    {
        if (viewModel is null)
        {
            return;
        }

        switch (status)
        {
            case GamePackageOperationReport.Reset:
                GamePackageOperationSpeedGraphHelper.ResetSpeedGraph(new SpeedGraphAdapter(DownloadSpeedGraph), ref downloadMaxSpeed, ref downloadSpeedGraphLastUpdateTimestamp);
                GamePackageOperationSpeedGraphHelper.ResetSpeedGraph(new SpeedGraphAdapter(InstallSpeedGraph), ref installMaxSpeed, ref installSpeedGraphLastUpdateTimestamp);
                return;
            case GamePackageOperationReport.Download:
                GamePackageOperationSpeedGraphHelper.UpdateSpeedGraph(new SpeedGraphAdapter(DownloadSpeedGraph), ref downloadMaxSpeed, ref downloadSpeedGraphLastUpdateTimestamp, viewModel.DownloadTotalBytes, viewModel.DownloadedBytes, viewModel.DownloadSpeedBytesPerSecond, Stopwatch.GetTimestamp(), SpeedGraphUpdateInterval);
                return;
            case GamePackageOperationReport.Install:
                GamePackageOperationSpeedGraphHelper.UpdateSpeedGraph(new SpeedGraphAdapter(InstallSpeedGraph), ref installMaxSpeed, ref installSpeedGraphLastUpdateTimestamp, viewModel.InstallTotalBytes, viewModel.InstalledBytes, viewModel.InstallSpeedBytesPerSecond, Stopwatch.GetTimestamp(), SpeedGraphUpdateInterval);
                return;
        }
    }
}