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
using DevWinUI;
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
                ResetSpeedGraph(DownloadSpeedGraph, ref downloadMaxSpeed, ref downloadSpeedGraphLastUpdateTimestamp, viewModel.DownloadTotalBytes);
                ResetSpeedGraph(InstallSpeedGraph, ref installMaxSpeed, ref installSpeedGraphLastUpdateTimestamp, viewModel.InstallTotalBytes);
                return;
            case GamePackageOperationReport.Download:
                UpdateSpeedGraph(DownloadSpeedGraph, ref downloadMaxSpeed, ref downloadSpeedGraphLastUpdateTimestamp, viewModel.DownloadTotalBytes, viewModel.DownloadedBytes, viewModel.DownloadSpeedBytesPerSecond);
                return;
            case GamePackageOperationReport.Install:
                UpdateSpeedGraph(InstallSpeedGraph, ref installMaxSpeed, ref installSpeedGraphLastUpdateTimestamp, viewModel.InstallTotalBytes, viewModel.InstalledBytes, viewModel.InstallSpeedBytesPerSecond);
                return;
        }
    }

    private void UpdateDownloadSpeedGraph(long totalBytes, long progressBytes, long speedBytesPerSecond)
    {
        UpdateSpeedGraph(DownloadSpeedGraph, ref downloadMaxSpeed, ref downloadSpeedGraphLastUpdateTimestamp, totalBytes, progressBytes, speedBytesPerSecond);
    }

    private void UpdateInstallSpeedGraph(long totalBytes, long progressBytes, long speedBytesPerSecond)
    {
        UpdateSpeedGraph(InstallSpeedGraph, ref installMaxSpeed, ref installSpeedGraphLastUpdateTimestamp, totalBytes, progressBytes, speedBytesPerSecond);
    }

    private static void ResetSpeedGraph(SpeedGraph speedGraph, ref ulong maxSpeed, ref long lastUpdateTimestamp, long totalBytes)
    {
        speedGraph.Normal();
        speedGraph.ResetGraph();
        speedGraph.Total = totalBytes > 0 ? (ulong)totalBytes : 1UL;
        speedGraph.MaxSpeed = 1UL;
        speedGraph.SetSpeed(0, 0);
        maxSpeed = 1;
        lastUpdateTimestamp = 0;
    }

    private static void UpdateSpeedGraph(SpeedGraph speedGraph, ref ulong maxSpeed, ref long lastUpdateTimestamp, long totalBytes, long progressBytes, long speedBytesPerSecond)
    {
        if (totalBytes <= 0)
        {
            ResetSpeedGraph(speedGraph, ref maxSpeed, ref lastUpdateTimestamp, totalBytes);
            return;
        }

        long current = Stopwatch.GetTimestamp();
        if (lastUpdateTimestamp is not 0 && Stopwatch.GetElapsedTime(lastUpdateTimestamp, current) < SpeedGraphUpdateInterval)
        {
            return;
        }

        lastUpdateTimestamp = current;
        speedGraph.Total = (ulong)totalBytes;
        ulong currentSpeed = speedBytesPerSecond < 0 ? 0UL : (ulong)speedBytesPerSecond;
        if (currentSpeed > maxSpeed)
        {
            maxSpeed = currentSpeed;
        }

        speedGraph.MaxSpeed = maxSpeed;
        double percent = Math.Clamp((double)progressBytes / totalBytes * 100D, 0D, 100D);
        speedGraph.SetSpeed(percent, currentSpeed);
    }
}