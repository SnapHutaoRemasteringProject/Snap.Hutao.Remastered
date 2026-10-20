// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
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

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

/// <summary>
/// Displays read-only, per-account historical aggregates. Only de-identified aggregates can
/// enter the optional user-initiated LLM request; repositories are never exposed to the model.
/// </summary>
[Service(ServiceLifetime.Transient)]
public sealed partial class AccountInsightsWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar, IXamlWindowHasInitSize, IXamlWindowClosedHandler
{
    private const string EndpointSettingKey = "AccountInsightsLlmEndpoint";
    private const string ModelSettingKey = "AccountInsightsLlmModel";

    private readonly CultureOptions cultureOptions;
    private readonly ILogger<AccountInsightsWindow> logger;
    private readonly IMetadataService metadataService;
    private readonly IGachaStatisticsFactory statisticsFactory;
    private readonly IUserService userService;
    private readonly IGachaLogRepository gachaRepository;
    private readonly ISpiralAbyssRepository abyssRepository;
    private readonly IRoleCombatRepository combatRepository;
    private readonly IHardChallengeRepository challengeRepository;
    private readonly CancellationTokenSource lifetime = new();

    private string? snapshotJson;
    private bool hasRecordedData;

    public AccountInsightsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider scoped = scope.ServiceProvider;

        cultureOptions = scoped.GetRequiredService<CultureOptions>();
        logger = scoped.GetRequiredService<ILogger<AccountInsightsWindow>>();
        userService = scoped.GetRequiredService<IUserService>();
        gachaRepository = scoped.GetRequiredService<IGachaLogRepository>();
        abyssRepository = scoped.GetRequiredService<ISpiralAbyssRepository>();
        combatRepository = scoped.GetRequiredService<IRoleCombatRepository>();
        challengeRepository = scoped.GetRequiredService<IHardChallengeRepository>();
        metadataService = scoped.GetRequiredService<IMetadataService>();
        statisticsFactory = scoped.GetRequiredService<IGachaStatisticsFactory>();

        Title = L("Title");
        EndpointBox.Text = LocalSetting.Get(EndpointSettingKey, string.Empty);
        ModelBox.Text = LocalSetting.Get(ModelSettingKey, string.Empty);

        PrivacyInfoButton.Click += (_, _) => PrivacyTeachingTip.IsOpen = true;
        CopyButton.Click += OnCopyClicked;
        GenerateButton.Click += OnGenerateClicked;
        TestButton.Click += OnTestClicked;
        RootGrid.Loaded += OnLoaded;

        this.InitializeController(scoped);
    }

    public FrameworkElement TitleBarCaptionAccess => DragRegion;

    public ImmutableArray<FrameworkElement> TitleBarPassthrough => [];

    public SizeInt32 InitSize => ScaledSizeInt32.CreateForWindow(1200, 800, this);

    public void OnWindowClosing(out bool cancel) => cancel = false;

    public void OnWindowClosed()
    {
        lifetime.Cancel();
        ApiKeyBox.Password = string.Empty;
        lifetime.Dispose();
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
        GenerateButton.IsEnabled = hasRecordedData && snapshotJson is not null && !ActivityRing.IsActive;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        try
        {
            if (await userService.GetCurrentUserAndUidAsync() is not { } userAndUid)
            {
                StatusText.Text = L("NoAccount");
                return;
            }

            // Each repository has its own scope. Preserve healthy datasets if another fails.
            CultureInfo culture = cultureOptions.CurrentCulture.Value;
            InsightLoadResult result = await Task.Run(() => CreateSeries(userAndUid.Uid.Value, culture), lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();

            hasRecordedData = result.Series.Any(item => item.Points.Count > 0);
            snapshotJson = hasRecordedData ? CreateAnonymousJson(result.Series) : null;

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
            UpdateGenerateEnabled();

            // Reuse the same metadata-backed statistics as the Gacha Log page.
            // The initial charts appear immediately; advanced statistics are optional.
            await LoadHuTaoStatisticsAsync(result);
        }
        catch (OperationCanceledException)
        {
            // The dialog was closed while the initial load was running.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize account insights");
            StatusText.Text = L("ReadError");
        }
    }


}
