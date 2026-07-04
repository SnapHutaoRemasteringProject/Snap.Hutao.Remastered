// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using DevWinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

[Service(ServiceLifetime.Transient)]
public sealed class SpeedGraphTestWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar,
    IXamlWindowHasInitSize
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly ObservableCollection<string> logEntries = [];
    private readonly Grid dragableGrid = new();
    private readonly TextBlock statusTextBlock = new();
    private readonly TextBlock currentStepTextBlock = new();
    private readonly ListView logListView = new();
    private readonly Button replayButton = new();
    private readonly SpeedGraph demoSpeedGraph = new();
    private bool hasAutoStarted;
    private bool isRunning;

    public SpeedGraphTestWindow(IServiceProvider serviceProvider)
    {
        Title = SH.ViewWindowSpeedGraphTestTitle;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = true;
        }

        BuildContent();

        IServiceScope scope = serviceProvider.CreateScope();
        this.InitializeController(scope.ServiceProvider);
    }

    public FrameworkElement TitleBarCaptionAccess { get => dragableGrid; }

    public ImmutableArray<FrameworkElement> TitleBarPassthrough { get => []; }

    public SizeInt32 InitSize { get => ScaledSizeInt32.CreateForWindow(1000, 760, this); }

    private void BuildContent()
    {
        Grid rootGrid = new()
        {
            RowSpacing = 12,
            Padding = new Thickness(0),
        };

        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition());
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        dragableGrid.Height = 32;
        dragableGrid.Children.Add(new TextBlock
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Text = SH.ViewWindowSpeedGraphTestTitle,
        });
        rootGrid.Children.Add(dragableGrid);

        StackPanel summaryPanel = new()
        {
            Margin = new Thickness(16, 0, 16, 0),
            Spacing = 4,
        };
        Grid.SetRow(summaryPanel, 1);
        summaryPanel.Children.Add(new TextBlock
        {
            Text = SH.ViewWindowSpeedGraphTestDescription,
        });
        statusTextBlock.Text = SH.ViewWindowSpeedGraphTestReady;
        summaryPanel.Children.Add(statusTextBlock);
        rootGrid.Children.Add(summaryPanel);

        Border graphBorder = new()
        {
            Margin = new Thickness(16, 0, 16, 0),
            Child = demoSpeedGraph,
        };
        demoSpeedGraph.Height = 180;
        Grid.SetRow(graphBorder, 2);
        rootGrid.Children.Add(graphBorder);

        StackPanel logPanel = new()
        {
            Margin = new Thickness(16, 0, 16, 0),
            Spacing = 8,
        };
        Grid.SetRow(logPanel, 3);
        currentStepTextBlock.Text = SH.ViewWindowSpeedGraphTestWaiting;
        logPanel.Children.Add(currentStepTextBlock);

        Border logBorder = new();
        logListView.Height = 360;
        logListView.ItemsSource = logEntries;
        logBorder.Child = logListView;
        logPanel.Children.Add(logBorder);
        rootGrid.Children.Add(logPanel);

        StackPanel buttonPanel = new()
        {
            Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Right,
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        Grid.SetRow(buttonPanel, 4);

        replayButton.Content = SH.ViewWindowSpeedGraphTestReplay;
        replayButton.Click += ReplayButton_Click;
        buttonPanel.Children.Add(replayButton);

        Button closeButton = new()
        {
            Content = SH.ViewWindowSpeedGraphTestClose,
        };
        closeButton.Click += CloseButton_Click;
        buttonPanel.Children.Add(closeButton);
        rootGrid.Children.Add(buttonPanel);

        Content = rootGrid;
        _ = StartIfNeededAsync();
    }

    private async Task StartIfNeededAsync()
    {
        if (hasAutoStarted)
        {
            return;
        }

        hasAutoStarted = true;
        await RunDemoAsync();
    }

    private async void ReplayButton_Click(object sender, RoutedEventArgs e)
    {
        await RunDemoAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task RunDemoAsync()
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        replayButton.IsEnabled = false;
        logEntries.Clear();
        statusTextBlock.Text = SH.ViewWindowSpeedGraphTestRunning;
        currentStepTextBlock.Text = SH.ViewWindowSpeedGraphTestWaiting;

        try
        {
            await GamePackageOperationSpeedGraphPlayback.RunAsync(new SpeedGraphAdapter(demoSpeedGraph), AppendLog, delay => Task.Delay(delay, cancellationTokenSource.Token), cancellationTokenSource.Token);
            statusTextBlock.Text = SH.ViewWindowSpeedGraphTestCompleted;
        }
        catch (OperationCanceledException)
        {
            statusTextBlock.Text = SH.ViewWindowSpeedGraphTestCanceled;
            AppendLog("测试流被取消");
        }
        finally
        {
            replayButton.IsEnabled = true;
            isRunning = false;
        }
    }

    private void AppendLog(string message)
    {
        logEntries.Add(message);
        currentStepTextBlock.Text = message;
        if (logEntries.Count > 0)
        {
            logListView.ScrollIntoView(logEntries[^1]);
        }
    }
}
