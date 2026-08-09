// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Shell;
using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Service.Game.AdvancedStart;
using Snap.Hutao.Remastered.Service.Game.AdvancedStart.Model;
using Snap.Hutao.Remastered.Service.Game.FileSystem;
using Snap.Hutao.Remastered.Service.Game.Locator;
using Snap.Hutao.Remastered.Service.Game.Package;
using Snap.Hutao.Remastered.Service.Game.PathAbstraction;
using Snap.Hutao.Remastered.Service.Game.Scheme;
using Snap.Hutao.Remastered.Service.Hutao;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.UI.Input.LowLevel;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using Snap.Hutao.Remastered.UI.Xaml.View.Window;
using Snap.Hutao.Remastered.ViewModel.User;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;

namespace Snap.Hutao.Remastered.ViewModel.Game;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Singleton)]
public sealed partial class LaunchGameViewModel : Abstraction.ViewModel, IViewModelSupportLaunchExecution, INavigationRecipient
{
    private readonly IGameLocatorFactory gameLocatorFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly IGameService gameService;
    private readonly IUserService userService;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly HutaoUserOptions hutaoUserOptions;
    private readonly IFileSystemPickerInteraction fileSystemPickerInteraction;
    private readonly AdvancedStartDelayedProgramStore store;
    private readonly IShellLinkInterop shellLinkInterop;

    [GeneratedConstructor]
    public partial LaunchGameViewModel(IServiceProvider serviceProvider);

    public partial GamePackageInstallViewModel GamePackageInstallViewModel { get; }

    public partial GamePackageViewModel GamePackageViewModel { get; }

    public partial LaunchStatusOptions LaunchStatusOptions { get; }

    public partial LowLevelKeyOptions LowLevelKeyOptions { get; }

    public partial LaunchOptions LaunchOptions { get; }

    public partial LaunchGameShared Shared { get; }

    public ImmutableArray<LaunchScheme> KnownSchemes { get; } = KnownLaunchSchemes.Values;

    public string AdvancedStartProgramPath
    {
        get => field;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    LaunchScheme? IViewModelSupportLaunchExecution.TargetScheme { get => TargetSchemeFilteredGameAccountsView.Scheme; }

    LaunchScheme? IViewModelSupportLaunchExecution.CurrentScheme { get => Shared.GetCurrentLaunchSchemeFromConfigurationFile(); }

    GameAccount? IViewModelSupportLaunchExecution.GameAccount { get => TargetSchemeFilteredGameAccountsView.View?.CurrentItem; }

    public LaunchSchemeFilteredGameAccountsView TargetSchemeFilteredGameAccountsView { get => field ??= new(IsViewUnloaded, gameService, taskContext, messenger); private set; }

    public IObservableProperty<NameValue<PlatformType>?> SelectedPlatformType { get => field ??= LaunchOptions.PlatformType.AsNameValue(LaunchOptions.PlatformTypes); }

    public IObservableProperty<GamePathEntry?> GamePathEntry { get => field ??= LaunchOptions.GamePathEntry.SetWithCondition(static (value, unloaded) => !unloaded.Value && value is not null, IsViewUnloaded); }

    public IReadOnlyObservableProperty<string> DisplayGamePath { get => field ??= Property.Observe(LaunchOptions.GamePathEntry, static entry => SH.FormatViewModelLaunchGameDisplayGamePath(entry?.Path)); }

    public IReadOnlyObservableProperty<bool> GamePathEntryValid { get => field ??= Property.Observe(LaunchOptions.GamePathEntry, static entry => !string.IsNullOrEmpty(entry?.Path)).WithValueChangedCallback(static (v, vm) => vm.HandleGamePathEntryChangeAsync().SafeForget(), this); }

    public IReadOnlyObservableProperty<bool> IsIslandConnected { get => GameLifeCycle.IsIslandConnected.AsReadOnly(); }

    // Delayed programs
    public ObservableCollection<AdvancedStartDelayedProgramEntry> Entries { get; private set => SetProperty(ref field, value); } = [];

    private AdvancedStartDelayedProgramEntry? selectedDelayedProgramEntry;

    public AdvancedStartDelayedProgramEntry? SelectedDelayedProgramEntry
    {
        get => selectedDelayedProgramEntry;
        set => SetProperty(ref selectedDelayedProgramEntry, value);
    }

    public bool IsDeveloperAndLoggedIn => hutaoUserOptions.IsLoggedIn && hutaoUserOptions.IsDeveloper;

    public async ValueTask<bool> ReceiveAsync(INavigationExtraData data, CancellationToken token)
    {
        if (!await Initialization.Task.ConfigureAwait(false))
        {
            return false;
        }

        if (data is LaunchGameExtraData { TypedData: { } uid })
        {
            return await userService.SetCurrentUserByUidAsync(uid).ConfigureAwait(false);
        }

        if (data is LaunchGameAutoLaunchData { TypedData: { } uidData })
        {
            if (uidData.Length > 0)
            {
                bool result = await userService.SetCurrentUserByUidAsync(uidData).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
            }

            // Initialization is complete; fire-and-forget the auto-launch
            LaunchAsync().SafeForget();
            return true;
        }

        return false;
    }

    [SuppressMessage("", "SH003")]
    public async Task HandleGamePathEntryChangeAsync()
    {
        try
        {
            using (await EnterCriticalSectionAsync().ConfigureAwait(false))
            {
                LaunchScheme? currentScheme = GamePathEntry.Value is not null
                    ? Shared.GetCurrentLaunchSchemeFromConfigurationFile()
                    : default;

                await taskContext.SwitchToMainThreadAsync();
                await TargetSchemeFilteredGameAccountsView.SetAsync(currentScheme).ConfigureAwait(true);
                await GamePackageViewModel.ReloadAsync().ConfigureAwait(true);
            }
        }
        catch (HutaoException ex)
        {
            messenger.Send(InfoBarMessage.Error(ex));
        }
    }

    ValueTask<BlockDeferral<PackageConvertStatus>> IViewModelSupportLaunchExecution.CreateConvertBlockDeferralAsync()
    {
        return BlockDeferral<PackageConvertStatus>.CreateAsync<LaunchGamePackageConvertDialog>(serviceProvider, static (state, dialog) => dialog.State = state);
    }

    private readonly object delayedSaveGate = new();
    private CancellationTokenSource? delayedSaveCts;
    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        AdvancedStartProgramPath = LocalSetting.Get(SettingKeys.LaunchAdvancedStartProgramPath, string.Empty);

        // Load delayed program entries
        try
        {
            Entries = store.Load();
        }
        catch
        {
            Entries = [];
        }

        WireEntries(Entries);

        if (LaunchOptions.GamePathEntries.Value.IsDefaultOrEmpty)
        {
            await serviceProvider.GetRequiredService<IGamePathService>().SilentLocateAllGamePathAsync().ConfigureAwait(false);
        }

        await HandleGamePathEntryChangeAsync().ConfigureAwait(false);
        Shared.ResumeLaunchExecutionAsync(this).SafeForget();

        return true;
    }

    private void WireEntries(ObservableCollection<AdvancedStartDelayedProgramEntry> entries)
    {
        entries.CollectionChanged += Entries_CollectionChanged;

        foreach (AdvancedStartDelayedProgramEntry entry in entries)
        {
            entry.PropertyChanged += Entry_PropertyChanged;
        }
    }

    private void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (object item in e.NewItems)
            {
                if (item is AdvancedStartDelayedProgramEntry added)
                {
                    added.PropertyChanged += Entry_PropertyChanged;
                }
            }
        }

        if (e.OldItems is not null)
        {
            foreach (object item in e.OldItems)
            {
                if (item is AdvancedStartDelayedProgramEntry removed)
                {
                    removed.PropertyChanged -= Entry_PropertyChanged;
                }
            }
        }

        ScheduleDelayedSave();
    }

    private void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Name / Path / DelaySeconds 任一变化都触发保存
        ScheduleDelayedSave();
    }

    private void ScheduleDelayedSave()
    {
        CancellationTokenSource? previous;

        lock (delayedSaveGate)
        {
            previous = delayedSaveCts;
            delayedSaveCts = new CancellationTokenSource();
        }

        previous?.Cancel();

        CancellationToken token = delayedSaveCts!.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), token).ConfigureAwait(false);
                store.Save(Entries);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                messenger.Send(InfoBarMessage.Error(ex));
            }
        });
    }

    [Command("IdentifyMonitorsCommand")]
    private static async Task IdentifyMonitorsAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Identify monitors", "LaunchGameViewModel.Command"));
        await IdentifyMonitorWindow.IdentifyAllMonitorsAsync(3).ConfigureAwait(false);
    }

    [Command("PickGamePathCommand")]
    private async Task PickGamePathAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Set game path by picker", "LaunchGameViewModel.Command"));
        if (await gameLocatorFactory.LocateSingleAsync(GameLocationSourceKind.Manual).ConfigureAwait(false) is not (true, var path))
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        LaunchOptions.PerformGamePathEntrySynchronization(path);
    }

    [Command("ResetGamePathCommand")]
    private void ResetGamePath()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Reset game path", "LaunchGameViewModel.Command"));
        LaunchOptions.GamePathEntry.Value = default;
        _ = 1;
    }

    [Command("RemoveGamePathEntryCommand")]
    private void RemoveGamePathEntry(GamePathEntry? entry)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Remove game path", "LaunchGameViewModel.Command"));
        LaunchOptions.RemoveGamePathEntry(entry);
    }

    [Command("RemoveAspectRatioCommand")]
    private void RemoveAspectRatio(AspectRatio? aspectRatio)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Remove aspect ratio", "LaunchGameViewModel.Command"));
        if (aspectRatio is null)
        {
            return;
        }

        if (aspectRatio.Equals(LaunchOptions.SelectedAspectRatio))
        {
            LaunchOptions.SelectedAspectRatio = default;
        }

        LaunchOptions.AspectRatios.Remove(aspectRatio);
    }

    [Command("LaunchCommand")]
    private async Task LaunchAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Launch game", "LaunchGameViewModel.Command"));

        if (LaunchOptions.AdvancedStartDelayedOnGameLaunch.Value)
        {
            Shared.LaunchAdvancedDelayedAsync().SafeForget();
        }

        UserAndUid? userAndUid = await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false);
        await Shared.DefaultLaunchExecutionAsync(this, userAndUid).ConfigureAwait(false);
    }

    [Command("CreateGameLaunchShortcutCommand")]
    private void CreateGameLaunchShortcut()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Create game launch shortcut", "LaunchGameViewModel.Command"));

        _ = shellLinkInterop.TryCreateGameLaunchShortcut()
            ? messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingActionComplete))
            : messenger.Send(InfoBarMessage.Warning(SH.ViewModelSettingCreateDesktopShortcutFailed));
    }

    [Command("ConvertCommand")]
    private async Task ConvertAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Convert game server", "LaunchGameViewModel.Command"));
        await Shared.ConvertLaunchExecutionAsync(this).ConfigureAwait(false);
    }

    [Command("DetectGameAccountCommand")]
    private async Task DetectGameAccountAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Detect registry game account", "LaunchGameViewModel.Command"));

        try
        {
            if (TargetSchemeFilteredGameAccountsView.Scheme is null)
            {
                messenger.Send(InfoBarMessage.Error(SH.ViewModelLaunchGameSchemeNotSelected));
                return;
            }

            if (TargetSchemeFilteredGameAccountsView.View is null)
            {
                return;
            }

            GameAccount? currentAccount = await gameService.DetectGameAccountAsync(TargetSchemeFilteredGameAccountsView.Scheme, async (suggestedName) =>
            {
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    LaunchGameAccountNameDialog dialog = await scope.ServiceProvider
                        .GetRequiredService<IContentDialogFactory>()
                        .CreateInstanceAsync<LaunchGameAccountNameDialog>(scope.ServiceProvider, suggestedName)
                        .ConfigureAwait(false);
                    return await dialog.GetInputNameAsync().ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            if (currentAccount is not null)
            {
                await taskContext.SwitchToMainThreadAsync();
                TargetSchemeFilteredGameAccountsView.View.MoveCurrentTo(currentAccount);
            }
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(ex));
        }
    }

    [Command("ModifyGameAccountCommand")]
    private async Task ModifyGameAccountAsync(GameAccount? gameAccount)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Modify registry game account", "LaunchGameViewModel.Command"));

        if (gameAccount is null)
        {
            return;
        }

        await gameService.ModifyGameAccountAsync(gameAccount, async originalName =>
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                LaunchGameAccountNameDialog dialog = await scope.ServiceProvider
                    .GetRequiredService<IContentDialogFactory>()
                    .CreateInstanceAsync<LaunchGameAccountNameDialog>(scope.ServiceProvider, originalName)
                    .ConfigureAwait(false);

                return await dialog.GetInputNameAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    [Command("RemoveGameAccountCommand")]
    private async Task RemoveGameAccountAsync(GameAccount? gameAccount)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Remove registry game account", "LaunchGameViewModel.Command"));

        if (gameAccount is null)
        {
            return;
        }

        await gameService.RemoveGameAccountAsync(gameAccount).ConfigureAwait(false);
    }

    [Command("OpenScreenshotFolderCommand")]
    private async Task OpenScreenshotFolderAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Open screenshot folder", "LaunchGameViewModel.Command"));

        const string LockTrace = $"{nameof(LaunchGameViewModel)}.{nameof(OpenScreenshotFolderAsync)}";
        if (LaunchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? gameFileSystem) is not GameFileSystemErrorKind.None)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(gameFileSystem);
        using (gameFileSystem)
        {
            Directory.CreateDirectory(gameFileSystem.ScreenShotDirectory);
            await Windows.System.Launcher.LaunchFolderPathAsync(gameFileSystem.ScreenShotDirectory);
        }
    }

    [Command("KillGameProcessCommand")]
    private async Task KillGameProcess()
    {
        if (!LaunchOptions.CanKillGameProcess.Value)
        {
            return;
        }

        await GameLifeCycle.TryKillGameProcessAsync(taskContext).ConfigureAwait(false);
    }

    [Command("LaunchAdvancedCommand")]
    private async Task LaunchAdvancedAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Launch advanced start program", "LaunchGameViewModel.Command"));

        try
        {
            string programPath = LocalSetting.Get(SettingKeys.LaunchAdvancedStartProgramPath, string.Empty);
            if (string.IsNullOrWhiteSpace(programPath))
            {
                messenger.Send(InfoBarMessage.Warning(SH.ViewModelLaunchGameAdvancedStartProgramPathNotSet));
                return;
            }

            if (!File.Exists(programPath))
            {
                messenger.Send(InfoBarMessage.Error(SH.ViewModelLaunchGameAdvancedStartProgramNotExists, programPath));
                return;
            }

            // Start using shell execute (no arguments)
            ProcessFactory.StartUsingShellExecute(string.Empty, programPath);
            messenger.Send(InfoBarMessage.Success(SH.ViewModelLaunchGameAdvancedStartProgramLaunched));

            if (LaunchOptions.AdvancedStartDelayedOnAdvancedStart.Value)
            {
                Shared.LaunchAdvancedDelayedAsync().SafeForget();
            }
        }
        catch (Exception ex)
        {
            // For UAC user cancel it's a ex too, need a way to...
            messenger.Send(InfoBarMessage.Error(ex));
        }
    }

    [Command("LaunchAdvancedDelayedCommand")]
    private Task LaunchAdvancedDelayedCommandAsync()
    {
        Shared.LaunchAdvancedDelayedAsync(CancellationToken).SafeForget();
        return Task.CompletedTask;
    }

    [Command("PickAdvancedStartProgramPathCommand")]
    private async Task PickAdvancedStartProgramPathAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Pick advanced start program", "LaunchGameViewModel.Command"));

        await taskContext.SwitchToBackgroundAsync();
        (bool picked, ValueFile file) = fileSystemPickerInteraction.PickFile(
            "Picker",
            "program",
            "*.exe");

        if (!picked)
        {
            return;
        }

        string path = file;

        // Persist can be done off-thread; UI-bound property update must be on UI thread.
        LocalSetting.Set(SettingKeys.LaunchAdvancedStartProgramPath, path);

        await taskContext.SwitchToMainThreadAsync();
        AdvancedStartProgramPath = path;
        messenger.Send(InfoBarMessage.Success(SH.ViewModelLaunchGameAdvancedStartProgramPathSaved));
    }

    [Command("PickBetterGenshinImpactPathCommand")]
    private async Task PickBetterGenshinImpactPathAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Pick BetterGI program", "LaunchGameViewModel.Command"));

        await taskContext.SwitchToBackgroundAsync();
        (bool picked, ValueFile file) = fileSystemPickerInteraction.PickFile(
            "Picker",
            "program",
            "*.exe");

        if (!picked)
        {
            return;
        }

        string path = file;

        await taskContext.SwitchToMainThreadAsync();
        LaunchOptions.BetterGenshinImpactPath.Value = path;
        messenger.Send(InfoBarMessage.Success(SH.ViewModelLaunchGameBetterGenshinImpactPathSaved));
    }

    // Delayed Programs Commands
    [Command("AddDelayedProgramCommand")]
    private async Task AddDelayedProgramAsync()
    {
        await taskContext.SwitchToBackgroundAsync();
        (bool ok, ValueFile file) = fileSystemPickerInteraction.PickFile("Picker", "program", "*.exe");
        if (!ok)
        {
            return;
        }

        string path = file;
        string name = ExecutableInfoHelper.GetFriendlyName(path);

        await taskContext.SwitchToMainThreadAsync();
        AdvancedStartDelayedProgramEntry entry = new(name, path, 0);
        Entries.Add(entry);
        SelectedDelayedProgramEntry = entry;
        store.Save(Entries);
    }

    [Command("RemoveDelayedProgramCommand")]
    private void RemoveDelayedProgram()
    {
        if (SelectedDelayedProgramEntry is null)
        {
            return;
        }

        Entries.Remove(SelectedDelayedProgramEntry);
        SelectedDelayedProgramEntry = null;
        store.Save(Entries);
    }

    [Command("SaveDelayedProgramCommand")]
    private void SaveDelayedProgram()
    {
        store.Save(Entries);
        messenger.Send(InfoBarMessage.Success(SH.ViewModelLaunchGameAdvancedStartProgramPathSaved));
    }

    [Command("EditDelayedProgramCommand")]
    private Task EditDelayedProgramAsync()
    {
        return PickDelayedProgramPathAsync(SelectedDelayedProgramEntry);
    }

    [Command("PickDelayedProgramPathCommand")]
    private async Task PickDelayedProgramPathAsync(AdvancedStartDelayedProgramEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        await taskContext.SwitchToBackgroundAsync();
        (bool ok, ValueFile file) = fileSystemPickerInteraction.PickFile("Picker", "program", "*.exe");
        if (!ok)
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        entry.Path = file;
        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            entry.Name = Path.GetFileNameWithoutExtension(entry.Path);
        }

        store.Save(Entries);
    }
}