// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.AI.Threading;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.ViewModel.AI;
using Snap.Hutao.Remastered.ViewModel.AI.Settings;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.GachaLog.Factory;
using Snap.Hutao.Remastered.ViewModel.GachaLog;
using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Immutable;
using Windows.Graphics;
using Microsoft.UI.Xaml.Controls.Charts;
using ChartSamples = Microsoft.UI.Xaml.Controls.Charts.Samples;
using WinUIChart = Microsoft.UI.Xaml.Controls.Charts.Chart;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.GachaLog;
using Snap.Hutao.Remastered.Service.HardChallenge;
using Snap.Hutao.Remastered.Service.RoleCombat;
using Snap.Hutao.Remastered.Service.SpiralAbyss;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

using Snap.Hutao.Remastered.Service.AccountInsights;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

/// <summary>
/// Displays read-only, per-account historical aggregates. Only de-identified aggregates can
/// enter the optional user-initiated LLM request; repositories are never exposed to the model.
/// </summary>
[Service(ServiceLifetime.Transient)]
public sealed partial class AccountInsightsWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar, IXamlWindowHasInitSize, IXamlWindowClosedHandler
{

    private readonly CultureOptions cultureOptions;
    private readonly ILogger<AccountInsightsWindow> logger;
    private readonly IMetadataService metadataService;
    private readonly IGachaStatisticsFactory statisticsFactory;
    private readonly IUserService userService;
    private readonly AccountInsightsHistoryService historyService;
    private readonly CancellationTokenSource lifetime = new();
    private readonly IServiceScope windowScope;
    private bool initializationStarted;
    private bool initializationCompleted;
    private bool closedResourcesDisposed;
    private bool waitingForAgentShutdown;
    private readonly AgentViewModel agentViewModel;
    private readonly AgentAccountContext agentAccountContext;

    private string? snapshotJson;
    private bool hasRecordedData;

    public AccountInsightsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        windowScope = serviceProvider.CreateScope();
        IServiceProvider scoped = windowScope.ServiceProvider;

        cultureOptions = scoped.GetRequiredService<CultureOptions>();
        logger = scoped.GetRequiredService<ILogger<AccountInsightsWindow>>();
        userService = scoped.GetRequiredService<IUserService>();
        historyService = scoped.GetRequiredService<AccountInsightsHistoryService>();
        metadataService = scoped.GetRequiredService<IMetadataService>();
        statisticsFactory = scoped.GetRequiredService<IGachaStatisticsFactory>();

        Title = L("Title");
        AgentThreadingContext.Initialize();
        Snap.Hutao.Remastered.Resource.Localization.AI.StringResourceProxy.Default.CurrentCulture = cultureOptions.CurrentCulture.Value;
        agentAccountContext = scoped.GetRequiredService<AgentAccountContext>();
        agentAccountContext.SystemPrompt = CreateSystemPrompt();
        agentViewModel = scoped.GetRequiredService<AgentViewModel>();
        AgentWorkspace.ViewModel = agentViewModel;
        AgentSettingsEditor.DataContext = agentViewModel;

        PrivacyInfoButton.Click += (_, _) => PrivacyTeachingTip.IsOpen = true;
        CopyButton.Click += OnCopyClicked;
        GenerateButton.Command = agentViewModel.GenerateAnalysisCommand;
        RootGrid.Loaded += OnLoaded;

        this.InitializeController(scoped);
    }

    public FrameworkElement TitleBarCaptionAccess => DragRegion;

    public ImmutableArray<FrameworkElement> TitleBarPassthrough => [];

    public SizeInt32 InitSize => ScaledSizeInt32.CreateForWindow(1400, 900, this);

    public void OnWindowClosing(out bool cancel) => cancel = false;

    public void OnWindowClosed()
    {
        RootGrid.Loaded -= OnLoaded;
        try { agentViewModel.SaveConversations(); }
        catch (Exception exception) { logger.LogWarning(exception, "Could not save agent conversations on close"); }
        finally
        {
            agentViewModel.Dispose();
            lifetime.Cancel();
            DisposeClosedResources();
        }
    }

    private string L(string name)
    {
        return Translate(name, cultureOptions.CurrentCulture.Value);
    }

    private static string Translate(string name, CultureInfo culture)
    {
        return SH.ResourceManager.GetString("ViewDialogAccountInsights" + name, culture) ?? name;
    }

    private void UpdateGenerateEnabled()
    {
        agentAccountContext.Snapshot = snapshotJson;
        AgentWorkspace.IsEnabled = true;
        GenerateButton.IsEnabled = hasRecordedData && snapshotJson is not null;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        initializationStarted = true;
        try
        {
            RootGrid.Loaded -= OnLoaded;
            if (await userService.GetCurrentUserAndUidAsync() is not { } userAndUid)
            {
                StatusText.Text = L("NoAccount");
                return;
            }

            lifetime.Token.ThrowIfCancellationRequested();
            agentViewModel.Initialize(agentAccountContext, userAndUid.Uid.Value.ToString(CultureInfo.InvariantCulture), windowScope.ServiceProvider.GetRequiredService<SettingsModelConfigurationViewModel>());
            AgentSettingsEditor.DataContext = agentViewModel;

            // Each repository has its own scope. Preserve healthy datasets if another fails.
            CultureInfo culture = cultureOptions.CurrentCulture.Value;
            InsightLoadResult result = await Task.Run(() => historyService.CreateSeries(userAndUid.Uid.Value, culture), lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();

            hasRecordedData = result.Series.Any(item => item.Points.Count > 0);
            snapshotJson = hasRecordedData ? AccountInsightsHistoryService.CreateAnonymousJson(result.Series) : null;

            ChartsPanel.Children.Clear();
            foreach (InsightSeries item in result.Series.Where(item => item.Points.Count > 0))
            {
                try
                {
                    ChartsPanel.Children.Add(CreateChart(item));
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to render account history chart: {Metric}", item.Metric);
                    ChartsPanel.Children.Add(new TextBlock
                    {
                        Text = L("ChartRenderError") + " (" + exception.GetType().Name + ")",
                        TextWrapping = TextWrapping.Wrap,
                    });
                }
            }

            string state = hasRecordedData ? L("Ready") : L("NoRecords");
            if (result.FailedSources.Count > 0)
            {
                state += " " + L("PartialError") + ": " +
                    string.Join(", ", result.FailedSources.Select(source => L(source)));
            }

            StatusText.Text = state;
            CopyButton.IsEnabled = hasRecordedData;

            // Keep analysis disabled until metadata-backed wish statistics have been
            // added, so an early click cannot send an incomplete statistics snapshot.
            await LoadHuTaoStatisticsAsync(result);
            UpdateGenerateEnabled();
        }
        catch (OperationCanceledException)
        {
            // The dialog was closed while the initial load was running.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize account insights");
            if (!lifetime.IsCancellationRequested)
            {
                StatusText.Text = L("ReadError");
            }
        }
        finally
        {
            initializationCompleted = true;
            DisposeClosedResources();
        }
    }

    private void DisposeClosedResources()
    {
        if (closedResourcesDisposed || !lifetime.IsCancellationRequested || (initializationStarted && !initializationCompleted))
        {
            return;
        }
        Task[] operations = agentViewModel.GetPendingOperations();
        if (operations.Length > 0)
        {
            if (!waitingForAgentShutdown)
            {
                waitingForAgentShutdown = true;
                _ = WaitForAgentShutdownAsync(operations);
            }
            return;
        }
        closedResourcesDisposed = true;
        windowScope.Dispose();
        lifetime.Dispose();
    }


    private async Task WaitForAgentShutdownAsync(Task[] operations)
    {
        try { await Task.WhenAll(operations); }
        catch (Exception exception) { logger.LogDebug("Agent shutdown completed with {ErrorType}", exception.GetType().Name); }
        finally { DisposeClosedResources(); }
    }

}
