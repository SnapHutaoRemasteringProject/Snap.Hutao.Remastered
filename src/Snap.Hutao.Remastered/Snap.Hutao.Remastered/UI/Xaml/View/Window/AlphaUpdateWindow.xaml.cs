// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Service.Update;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AlphaUpdateWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar,
    IXamlWindowHasInitSize,
    IXamlWindowClosedHandler
{
    private readonly AlphaUpdateHistoryClient client;
    private readonly IAppActivation appActivation;
    private readonly ILogger<AlphaUpdateWindow> logger;
    private readonly string? previousCommit;
    private readonly bool previewDev;
    private readonly Dictionary<string, Type> modulePages = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource lifetime = CancellationTokenSource.CreateLinkedTokenSource(XamlApplicationLifetime.ShutdownToken);
    private string? currentCommit;
    private CancellationTokenSource? commitLoad;
    private CancellationTokenSource? fileLoad;
    private string? baseline;
    private AlphaGitHubCommit? selectedCommit;
    private string? selectedParent;
    private int olderPage = 1;

    public AlphaUpdateWindow(IServiceProvider serviceProvider, string? previousCommit, string? currentCommit)
        : this(serviceProvider, previousCommit, currentCommit, previewDev: false)
    {
    }

    private AlphaUpdateWindow(IServiceProvider serviceProvider, string? previousCommit, string? currentCommit, bool previewDev)
    {
        this.previousCommit = previousCommit;
        this.currentCommit = currentCommit;
        this.previewDev = previewDev;
        client = serviceProvider.GetRequiredService<AlphaUpdateHistoryClient>();
        appActivation = serviceProvider.GetRequiredService<IAppActivation>();
        logger = serviceProvider.GetRequiredService<ILogger<AlphaUpdateWindow>>();

        InitializeComponent();
        BuildLabel.Text = previewDev ? "dev" : $"{HutaoRuntime.Version}";
        if (currentCommit is not null)
        {
            BuildLabel.Text += $"  ·  {currentCommit[..8]}";
        }

        CompareLink.NavigateUri = AlphaUpdateHistoryClient.DevPage;
        DetailStatus.Text = SH.ViewWindowAlphaUpdateSelectCommit;

        Hero.Modules =
        [
            CreateHeroModule<LaunchGamePage>("LaunchGame", SH.ViewLaunchGameHeader),
            CreateHeroModule<GachaLogPage>("GachaLog", SH.ViewGachaLogHeader),
            CreateHeroModule<AchievementPage>("Achievement", SH.ViewAchievementHeader),
            CreateHeroModule<DailyNotePage>("DailyNote", SH.ViewDailyNoteHeader),
            CreateHeroModule<AvatarPropertyPage>("AvatarProperty", SH.ViewAvatarPropertyHeader),
            CreateHeroModule<BackpackPage>("Backpack", SH.ViewBackpackHeader),
            CreateHeroModule<CultivationPage>("Cultivation", SH.ViewCultivationHeader),
            CreateHeroModule<SpiralAbyssRecordPage>("SpiralAbyss", SH.ViewSpiralAbyssHeader),
            CreateHeroModule<RoleCombatPage>("RoleCombat", SH.ViewRoleCombatHeader),
            CreateHeroModule<HardChallengePage>("HardChallenge", SH.ViewHardChallengeHeader),
            CreateHeroModule<WikiAvatarPage>("WikiAvatar", SH.ViewWikiAvatarHeader),
            CreateHeroModule<WikiWeaponPage>("WikiWeapon", SH.ViewWikiWeaponHeader),
            CreateHeroModule<WikiMonsterPage>("WikiMonster", SH.ViewWikiMonsterHeader),
            CreateHeroModule<AnnouncementPage>("Announcement", SH.ViewAnnouncementHeader),
            CreateHeroModule<HutaoPassportPage>("Passport", SH.ViewHutaoPassportHeader),
            CreateHeroModule<FeedbackPage>("Feedback", SH.ViewFeedbackHeader),
        ];
        Hero.RevealTargets.Add(WelcomeTitle);
        Hero.RevealTargets.Add(BuildLabel);
        Hero.RevealTargets.Add(CompareLink);

        this.InitializeController(serviceProvider.CreateScope().ServiceProvider);
        Title = SH.ViewWindowAlphaUpdateTitle;
    }

    public FrameworkElement TitleBarCaptionAccess => DragRegion;

    public ImmutableArray<FrameworkElement> TitleBarPassthrough => [];

    public SizeInt32 InitSize => ScaledSizeInt32.CreateForWindow(1080, 710, this);

    public static void ShowDevPreview(IServiceProvider serviceProvider)
    {
        AlphaUpdateWindow window = new(serviceProvider, previousCommit: null, currentCommit: null, previewDev: true);
        window.Activate();
    }

    public void OnWindowClosing(out bool cancel)
    {
        cancel = false;
        lifetime.Cancel();
    }

    public void OnWindowClosed()
    {
        Hero.ModuleInvoked -= OnHeroModuleInvoked;
        lifetime.Dispose();
    }

    private WelcomeHeroModule CreateHeroModule<TPage>(string iconName, string title)
        where TPage : Microsoft.UI.Xaml.Controls.Page
    {
        Type pageType = typeof(TPage);
        string tag = pageType.Name;
        modulePages.Add(tag, pageType);
        return new($"ms-appx:///Resource/Navigation/{iconName}.png", tag, title);
    }

    private void OnHeroModuleInvoked(object? sender, string navigationTag)
    {
        if (!lifetime.IsCancellationRequested
            && modulePages.TryGetValue(navigationTag, out Type? pageType)
            && appActivation is IAppActivationActionHandlersAccess actions)
        {
            actions.NavigateMainWindowAsync(pageType).SafeForget();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        RootGrid.Loaded -= OnLoaded;
        DispatcherQueue.TryEnqueue(() => XamlWindowRegionRects.Update(this));
        await LoadHistoryAsync();
    }

    private async void OnRetryClicked(object sender, RoutedEventArgs args) => await LoadHistoryAsync();

    private async void OnOlderClicked(object sender, RoutedEventArgs args) => await LoadOlderAsync();

    private async Task LoadHistoryAsync()
    {
        RetryButton.Visibility = Visibility.Collapsed;
        if (currentCommit is null && !previewDev)
        {
            HistoryStatus.Text = SH.ViewWindowAlphaUpdateMissingRevision;
            return;
        }

        HistoryStatus.Text = SH.ViewControlLoadingText;
        await LoadAsync(async token =>
        {
            string current = currentCommit ?? await client.GetDevHeadAsync(token);
            token.ThrowIfCancellationRequested();
            if (currentCommit is null)
            {
                currentCommit = current;
                BuildLabel.Text = $"dev  ·  {current[..8]}";
            }

            if (baseline is { } existingBaseline)
            {
                await LoadUpdateCommitsAsync(existingBaseline, current, token);
                return;
            }

            (string sha, string label) = await client.GetBaselineAsync(previousCommit, token);
            token.ThrowIfCancellationRequested();
            baseline = sha;
            RangeLabel.Text = $"{label} → {current[..8]}";
            CompareLink.NavigateUri = AlphaUpdateHistoryClient.ComparePage(sha, current);
            CompareLink.Content = SH.ViewWindowAlphaUpdateOpenComparison;

            await Task.WhenAll(LoadUpdateCommitsAsync(sha, current, token), LoadOlderAsync());
        }, () =>
        {
            HistoryStatus.Text = SH.ViewWindowAlphaUpdateLoadFailed;
            RetryButton.Visibility = Visibility.Visible;
        }, lifetime.Token);
    }

    private async Task LoadUpdateCommitsAsync(string baseline, string current, CancellationToken token)
    {
        List<AlphaGitHubCommit> commits = await client.GetUpdateCommitsAsync(baseline, current, token);
        token.ThrowIfCancellationRequested();
        HistoryStatus.Text = commits.Count == 0 ? SH.ViewWindowAlphaUpdateNoNewCommits : string.Empty;
        UpdateCommits.ItemsSource = ItemsSourceHelper.Create(commits, commits.Count == 0);
        if (commits.Count > 0 && selectedCommit is null)
        {
            UpdateCommits.SelectedIndex = 0;
        }
    }

    private async Task LoadOlderAsync()
    {
        if (baseline is not { } sha)
        {
            return;
        }

        OlderButton.IsEnabled = false;
        OlderStatus.Text = SH.ViewControlLoadingText;
        await LoadAsync(async token =>
        {
            (AlphaGitHubCommit[] commits, bool hasNext) = await client.GetOlderCommitsAsync(sha, olderPage, token);
            token.ThrowIfCancellationRequested();
            foreach (AlphaGitHubCommit commit in commits)
            {
                OlderCommits.Items.Add(commit);
            }

            ++olderPage;
            OlderStatus.Text = string.Empty;
            OlderButton.Visibility = hasNext ? Visibility.Visible : Visibility.Collapsed;
        }, () =>
        {
            OlderStatus.Text = SH.ViewWindowAlphaUpdateLoadFailed;
            OlderButton.Visibility = Visibility.Visible;
        }, lifetime.Token);
        OlderButton.IsEnabled = true;
    }

    private async void OnCommitSelected(object sender, SelectionChangedEventArgs args)
    {
        ListView list = (ListView)sender;
        if (list.SelectedItem is not AlphaGitHubCommit commit)
        {
            return;
        }

        (list == UpdateCommits ? OlderCommits : UpdateCommits).SelectedIndex = -1;
        commitLoad?.Cancel();
        fileLoad?.Cancel();
        selectedCommit = commit;
        selectedParent = null;
        FileList.ItemsSource = null;
        FileList.Visibility = Visibility.Collapsed;
        Diff.Visibility = Visibility.Collapsed;
        CommitMessage.Text = commit.Commit.Message;
        CommitLink.NavigateUri = AlphaUpdateHistoryClient.CommitPage(commit.Sha);
        CommitLink.Visibility = Visibility.Visible;
        DetailStatus.Text = SH.ViewControlLoadingText;

        using CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        commitLoad = operation;
        await LoadAsync(async token =>
        {
            (string? parent, List<AlphaGitHubFile> files) = await client.GetChangedFilesAsync(commit.Sha, token);
            token.ThrowIfCancellationRequested();
            selectedParent = parent;
            DetailStatus.Text = files.Count == 0 ? SH.ViewWindowAlphaUpdateNoChangedFiles : string.Empty;
            if (files.Count > 0)
            {
                FileList.ItemsSource = ItemsSourceHelper.Create(files, false);
                FileList.Visibility = Visibility.Visible;
                FileList.SelectedIndex = 0;
            }
        }, () => DetailStatus.Text = SH.ViewWindowAlphaUpdateLoadFailed, operation.Token);

        if (commitLoad == operation)
        {
            commitLoad = null;
        }
    }

    private async void OnFileSelected(object sender, SelectionChangedEventArgs args)
    {
        if (FileList.SelectedItem is not AlphaGitHubFile file || selectedCommit is not { } commit)
        {
            return;
        }

        fileLoad?.Cancel();
        using CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        fileLoad = operation;
        Diff.Visibility = Visibility.Collapsed;
        DetailStatus.Text = SH.ViewControlLoadingText;

        await LoadAsync(async token =>
        {
            (string OldText, string NewText)? diff = await client.GetDiffAsync(commit.Sha, selectedParent, file, token);
            token.ThrowIfCancellationRequested();
            if (diff is not { } text)
            {
                DetailStatus.Text = SH.ViewWindowAlphaUpdateNoTextDiff;
                return;
            }

            Diff.Filename = file.Filename;
            Diff.OldText = text.OldText;
            Diff.NewText = text.NewText;
            Diff.Visibility = Visibility.Visible;
            DetailStatus.Text = string.Empty;
        }, () => DetailStatus.Text = SH.ViewWindowAlphaUpdateDiffFailed, operation.Token);

        if (fileLoad == operation)
        {
            fileLoad = null;
        }
    }

    private async Task LoadAsync(Func<CancellationToken, Task> load, Action onError, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            await load(token);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or OperationCanceledException)
        {
            if (token.IsCancellationRequested)
            {
                Debug.WriteLine("Canceled an Alpha update history request.");
                return;
            }

            logger.LogWarning(exception, "Could not load Alpha update history from GitHub");
            onError();
        }
    }
}
