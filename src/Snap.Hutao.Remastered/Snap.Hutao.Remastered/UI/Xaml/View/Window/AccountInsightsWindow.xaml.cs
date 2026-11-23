// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.AI.Threading;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.ViewModel.AI;
using Snap.Hutao.Remastered.ViewModel.User;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.GachaLog.Factory;
using System.Collections.Immutable;
using Windows.Graphics;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.User;
using System.Globalization;

using Snap.Hutao.Remastered.Service.AccountInsights;
using Snap.Hutao.Remastered.UI.Xaml.Control;

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
    private readonly ITaskContext taskContext;
    private readonly CancellationTokenSource lifetime = new();
    private readonly IServiceScope windowScope;
    private bool initializationStarted;
    private bool initializationCompleted;
    private bool closedResourcesDisposed;
    private bool waitingForAgentShutdown;
    private readonly AgentViewModel agentViewModel;
    private readonly AgentAccountContext agentAccountContext;

    private string? snapshotJson;

    public AccountInsightsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        taskContext = TaskContext.GetForDependencyObject(RootGrid);
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

    private void OnAgentSettingsLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is AgentSettingsView editor)
        {
            editor.DataContext = agentViewModel;
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        initializationStarted = true;
        CancellationToken token = lifetime.Token;
        bool agentReady = false;
        string operation = "GetCurrentUserAndUidAsync";
        try
        {
            RootGrid.Loaded -= OnLoaded;
            UserAndUid? userAndUid = await userService.GetCurrentUserAndUidAsync(token);
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            if (userAndUid is null)
            {
                StatusText.Text = L("NoAccount");
                return;
            }

            try
            {
                agentViewModel.Initialize(userAndUid.Uid.Value.ToString(CultureInfo.InvariantCulture));
                agentReady = true;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to initialize the agent workspace");
            }

            operation = nameof(AccountInsightsHistoryService.CreateSeries);
            CultureInfo culture = cultureOptions.CurrentCulture.Value;
            InsightLoadResult result = await Task.Run(() => historyService.CreateSeries(userAndUid.Uid.Value, culture), token);
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();

            operation = nameof(AddChart);
            ChartsPanel.Children.Clear();
            foreach (InsightSeries item in result.Series.Where(item => item.Points.Count > 0))
            {
                AddChart(item);
            }

            bool hasRecordedData = result.Series.Any(item => item.Points.Count > 0);
            string state = hasRecordedData ? L("Ready") : L("NoRecords");
            if (result.Failures.Count > 0)
            {
                state += " " + L("PartialError") + " (" +
                    string.Join(", ", result.Failures.Select(failure => L(failure.Source)).Distinct()) + ")";
                foreach (InsightLoadFailure failure in result.Failures)
                {
                    ShowLoadError(L(failure.Source), failure.Error);
                }
            }

            StatusText.Text = state;
            if (hasRecordedData)
            {
                try
                {
                    snapshotJson = AccountInsightsHistoryService.CreateAnonymousJson(result.Series);
                    CopyButton.IsEnabled = true;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to create the account statistics snapshot");
                    ShowLoadError(nameof(AccountInsightsHistoryService.CreateAnonymousJson), exception);
                }
            }

            // Keep analysis disabled until metadata-backed wish statistics have been
            // added, so an early click cannot send an incomplete statistics snapshot.
            await LoadHuTaoStatisticsAsync(result, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The dialog was closed while the initial load was running.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize account insights at {Operation}", operation);
            await taskContext.SwitchToMainThreadAsync();
            if (!token.IsCancellationRequested)
            {
                StatusText.Text = L("ReadError");
                ShowLoadError(operation, exception);
            }
        }
        finally
        {
            await taskContext.SwitchToMainThreadAsync();
            if (agentReady && !token.IsCancellationRequested)
            {
                agentAccountContext.Snapshot = snapshotJson;
                AgentWorkspace.IsEnabled = true;
                GenerateButton.IsEnabled = snapshotJson is not null;
            }

            initializationCompleted = true;
            DisposeClosedResources();
        }
    }

    private void ShowLoadError(string source, Exception exception)
    {
        Exception cause = exception.GetBaseException();
        string detail = $"{source} — {cause.GetType().Name} (0x{cause.HResult:X8}){Environment.NewLine}{cause.Message}";
        ErrorDetailsText.Text = string.IsNullOrEmpty(ErrorDetailsText.Text)
            ? detail
            : ErrorDetailsText.Text + Environment.NewLine + Environment.NewLine + detail;
        ErrorDetailsText.Visibility = Visibility.Visible;
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
