// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service.Update;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
    private readonly AlphaUpdateRange? updateRange;
    private readonly bool saveRange;
    private readonly ObservableCollection<SidebarItemModel> fileItems = [];
    private readonly Dictionary<SidebarItemModel, AlphaGitHubFile> changedFiles = [];
    private readonly Dictionary<string, Type> modulePages = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource lifetime = CancellationTokenSource.CreateLinkedTokenSource(XamlApplicationLifetime.ShutdownToken);
    private CancellationTokenSource? commitLoad;
    private CancellationTokenSource? fileLoad;
    private string? baseline;
    private AlphaGitHubCommit? selectedCommit;
    private string? selectedParent;
    private int olderPage = 1;

    internal AlphaUpdateWindow(IServiceProvider serviceProvider, AlphaUpdateRange? updateRange, bool saveRange = false)
    {
        this.updateRange = updateRange;
        this.saveRange = saveRange;
        client = serviceProvider.GetRequiredService<AlphaUpdateHistoryClient>();
        appActivation = serviceProvider.GetRequiredService<IAppActivation>();
        logger = serviceProvider.GetRequiredService<ILogger<AlphaUpdateWindow>>();

        SidebarViewItems = new(fileItems);
        InitializeComponent();
        BuildLabel.Text = $"{HutaoRuntime.Version}";
        if (updateRange is { CurrentCommit: { } current })
        {
            BuildLabel.Text += $"  ·  {current[..8]}";
        }

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

    public SidebarViewMenuSource SidebarViewItems { get; }

    public static void ShowBuildPreview(IServiceProvider serviceProvider)
    {
        string? current = AlphaUpdateHistoryClient.GetBuildCommit();
        string previous = LocalSetting.Get(SettingKeys.LastAlphaCommit, string.Empty);
        AlphaUpdateWindow window = new(serviceProvider, AlphaUpdateRange.ForBuild(current, previous));
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
        SidebarViewItems.Dispose();
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
        if (updateRange is not { } range)
        {
            HistoryStatus.Text = SH.ViewWindowAlphaUpdateMissingRevision;
            return;
        }

        string current = range.CurrentCommit;
        HistoryStatus.Text = SH.ViewControlLoadingText;
        await LoadAsync(async token =>
        {
            if (baseline is { } existingBaseline)
            {
                await LoadUpdateCommitsAsync(existingBaseline, current, token);
                return;
            }

            (string sha, string label) = await client.GetBaselineAsync(range.BaselineCommit, token);
            token.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(range.BaselineLabel))
            {
                label = range.BaselineLabel;
            }
            else if (saveRange)
            {
                (range with { BaselineCommit = sha, BaselineLabel = label }).Save();
            }

            baseline = sha;
            RangeLabel.Text = $"{label} → {current[..8]}";
            CompareLink.NavigateUri = AlphaUpdateHistoryClient.ComparePage(sha, current);
            CompareLink.Visibility = Visibility.Visible;

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
        FileSidebar.SelectedItem = null;
        FileSidebar.Visibility = Visibility.Collapsed;
        changedFiles.Clear();
        fileItems.Clear();
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
            if (BuildFileTree(files) is { } first)
            {
                FileSidebar.Visibility = Visibility.Visible;
                FileSidebar.SelectedItem = first;
                await LoadFileDiffAsync(commit, changedFiles[first]);
            }
        }, () => DetailStatus.Text = SH.ViewWindowAlphaUpdateLoadFailed, operation.Token);

        if (commitLoad == operation)
        {
            commitLoad = null;
        }
    }

    private SidebarItemModel? BuildFileTree(List<AlphaGitHubFile> files)
    {
        Dictionary<string, SidebarItemModel> folders = new(StringComparer.Ordinal);
        List<SidebarItemModel> roots = [];
        SidebarItemModel? first = null;
        foreach (AlphaGitHubFile file in files.OrderBy(static file => file.Filename, StringComparer.Ordinal))
        {
            string[] segments = file.Filename.Split('/');
            ICollection<SidebarItemModel> siblings = roots;
            string path = string.Empty;
            foreach (string segment in segments.AsSpan(0, segments.Length - 1))
            {
                path = path.Length == 0 ? segment : $"{path}/{segment}";
                if (!folders.TryGetValue(path, out SidebarItemModel? folder))
                {
                    folder = new()
                    {
                        Text = segment,
                        ToolTip = path,
                        IconSource = new FontIconSource
                        {
                            FontFamily = new FontFamily("Segoe UI Emoji"),
                            FontSize = 14,
                            Glyph = "\U0001F4C1"
                        },
                        Children = [],
                        IsExpanded = true,
                    };
                    folders.Add(path, folder);
                    siblings.Add(folder);
                }

                siblings = folder.Children!;
            }

            SidebarItemModel leaf = new() { Text = segments[^1], ToolTip = file.Filename, IconSource = GetFileIcon(segments[^1]) };
            siblings.Add(leaf);
            changedFiles.Add(leaf, file);
            first ??= leaf;
        }

        foreach (SidebarItemModel root in roots)
        {
            CompactFolderPath(root);
            fileItems.Add(root);
        }

        return first;
    }

    private static void CompactFolderPath(SidebarItemModel item)
    {
        while (item.Children is { Count: 1 } children && children[0] is { Children: { } descendants } folder)
        {
            item.Text = $"{item.Text}/{folder.Text}";
            item.ToolTip = folder.ToolTip;
            item.Children = descendants;
        }

        if (item.Children is { } remaining)
        {
            foreach (SidebarItemModel child in remaining)
            {
                CompactFolderPath(child);
            }
        }
    }

    private static IconSource GetFileIcon(string filename)
    {
        return Path.GetExtension(filename).ToLowerInvariant() switch
        {
            ".cs" or ".xaml" or ".xml" or ".csproj" or ".props" or ".targets" or ".resx"
                or ".c" or ".cc" or ".cpp" or ".h" or ".hpp" or ".rs" or ".go" or ".py"
                or ".js" or ".jsx" or ".ts" or ".tsx" or ".css" or ".html" or ".sql"
                or ".ps1" or ".psm1" or ".sh" or ".bat" or ".cmd"
                or ".json" or ".yml" or ".yaml" or ".toml"
                => new FontIconSource
                {
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 14,
                    Glyph = "\uE943"
                },
            _ => new SymbolIconSource { Symbol = Symbol.Document },
        };
    }

    private async void OnFileInvoked(object? sender, DevWinUI.ItemInvokedEventArgs args)
    {
        if (sender is SidebarItem { Item: SidebarItemModel item }
            && changedFiles.TryGetValue(item, out AlphaGitHubFile? file)
            && selectedCommit is { } commit)
        {
            await LoadFileDiffAsync(commit, file);
        }
    }

    private async Task LoadFileDiffAsync(AlphaGitHubCommit commit, AlphaGitHubFile file)
    {
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

            Diff.Filename = Path.GetFileName(file.Filename);
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
