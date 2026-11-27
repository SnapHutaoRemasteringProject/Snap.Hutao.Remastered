using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Packaging;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Plugin;
using System.IO;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginPage : ScopedPage
{
    private PluginService service = null!;
    private IServiceProvider services = null!;
    private InstalledPlugin[] entries = [];
    private bool busy;
    private bool unloaded;

    public PluginPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RunAsync(RefreshAsync);
        Unloaded += (_, _) =>
        {
            unloaded = true;
            if (service is not null) service.Changed -= OnPluginsChanged;
        };
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<PluginViewModel>();
        services = XamlRoot.XamlContext()!.ServiceProvider;
        service = services.GetRequiredService<PluginService>();
        service.AttachRoot(XamlRoot);
        service.Changed += OnPluginsChanged;
        this.DataContext<PluginViewModel>()!.MarkReady();
    }

    private InstalledPlugin? Selected => Packages.SelectedIndex is int index && index >= 0 && index < entries.Length ? entries[index] : null;

    private async void OnPluginsChanged()
    {
        if (!unloaded && !busy) await RunAsync(RefreshAsync);
    }

    private async Task RefreshAsync()
    {
        string? selectedId = Selected?.Id;
        InstalledPlugin[] scanned = await service.ScanAsync();
        if (unloaded) return;
        entries = scanned;
        Packages.Items.Clear();
        foreach (InstalledPlugin entry in entries)
        {
            string status = entry.Error is not null ? entry.Error : service.IsActive(entry.Id) ? SH.ViewPagePluginStatusEnabled : SH.ViewPagePluginStatusDisabled;
            Packages.Items.Add($"{entry.Manifest?.Name ?? entry.Id} ({entry.Manifest?.Version}) — {status}");
        }
        Packages.SelectedIndex = Array.FindIndex(entries, item => string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        if (Packages.SelectedIndex < 0 && entries.Length != 0) Packages.SelectedIndex = 0;
        UpdateSelection();
        if (service.RecoveryMode && !Status.IsOpen)
            Show(InfoBarSeverity.Warning, SH.GetString("ViewPagePluginRecovery") ?? "Automatic plugin startup was skipped.");
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelection();

    private void UpdateSelection()
    {
        if (CommandsPanel is null || service is null) return;
        CommandsPanel.Children.Clear();
        InstalledPlugin? item = Selected;
        Actions.IsEnabled = !busy && item is { Conflict: false };
        if (item is null) { Details.Text = string.Empty; return; }
        Details.Text = $"{item.Id}\n{item.Manifest?.Description}\nSHA256: {item.Hash}\n{item.Error}";
        try
        {
            PluginDiskState state = PluginPackageStore.ReadState(item.Id);
            if (state.PendingHash is not null) Details.Text += "\nPending update SHA256: " + state.PendingHash;
            if (state.BackupHash is not null) Details.Text += "\nRollback SHA256: " + state.BackupHash;
            if (state.LastError is not null) Details.Text += "\n" + state.LastError;
        }
        catch (Exception error) { Details.Text += "\n" + error.Message; }
        if (!service.IsActive(item.Id)) return;
        foreach (CommandContribution command in service.Commands(item.Id))
        {
            Button button = new() { Content = command.Title };
            string id = item.Id, commandId = command.Id;
            button.Click += async (_, _) => await RunAsync(() => service.ExecuteAsync(id, commandId));
            CommandsPanel.Children.Add(button);
        }
    }

    private void Show(InfoBarSeverity severity, string message)
    {
        if (unloaded) return;
        Status.Severity = severity;
        Status.Message = message;
        Status.IsOpen = true;
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy || unloaded) return;
        busy = true;
        Packages.IsEnabled = Actions.IsEnabled = false;
        try { await action(); }
        catch (Exception error) { Show(InfoBarSeverity.Error, error.ToString()); }
        finally
        {
            busy = false;
            if (!unloaded) { Packages.IsEnabled = true; UpdateSelection(); }
        }
    }

    private string? PickPackage()
    {
        IFileSystemPickerInteraction picker = services.GetRequiredService<IFileSystemPickerInteraction>();
        var result = picker.PickFile(SH.ServicePluginPickFileTitle, "HutaoPlugin", "*.hutao;*.hutaodisabled");
        return result.IsOk ? result.Value.ToString() : null;
    }

    private async Task<bool> ConfirmTrustAsync(PluginManifest manifest, string hash)
    {
        string gameLibraries = manifest.GameLibraries.Count == 0 ? string.Empty :
            "\n\n" + (SH.GetString("ViewPagePluginGameLibrariesWarning") ??
                "This package also requests loading third-party libraries into the game. Compatibility and account safety are not guaranteed.") +
            "\n" + string.Join("\n", manifest.GameLibraries);
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = SH.GetString("ViewPagePluginTrustTitle") ?? "Trust native plugin?",
            Content = $"{manifest.Name} ({manifest.Id}) {manifest.Version}\nSHA256: {hash}\nCapabilities: {string.Join(", ", manifest.Capabilities.OrderBy(value => value, StringComparer.Ordinal))}\n\n"
                + (SH.GetString("ViewPagePluginTrustWarning") ?? "Native code runs with the same permissions as the application.")
                + (Environment.IsPrivilegedProcess ? "\n" + (SH.GetString("ViewPagePluginElevatedWarning") ?? "This process is elevated.") : string.Empty)
                + gameLibraries,
            PrimaryButtonText = SH.GetString("ViewPagePluginTrustButton") ?? "Trust",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async void InstallClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        if (PickPackage() is not { } path) return;
        VerifiedPackage verified = await Task.Run(() => PluginPackage.Verify(path));
        bool enable = Path.GetExtension(path).Equals(".hutao", StringComparison.OrdinalIgnoreCase);
        if (enable && !await ConfirmTrustAsync(verified.Manifest, verified.Sha256)) return;
        await service.InstallAsync(path, verified);
        if (enable)
        {
            try { await service.EnableAsync(verified.Manifest.Id, verified.Sha256); }
            finally { await RefreshAsync(); }
        }
        else await RefreshAsync();
    });

    private async void RefreshClick(object sender, RoutedEventArgs e) => await RunAsync(RefreshAsync);

    private async void OpenDirectoryClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        await service.ScanAsync();
        await Windows.System.Launcher.LaunchFolderAsync(await Windows.Storage.StorageFolder.GetFolderFromPathAsync(PluginPackageStore.Root));
    });

    private async void EnableClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        if (Selected is not { Manifest: { } manifest, Hash: { } hash } item) throw new InvalidOperationException("Incompatible plugin.");
        if (service.IsActive(item.Id)) return;
        if (!await ConfirmTrustAsync(manifest, hash)) return;
        try { await service.EnableAsync(item.Id, hash); }
        finally { await RefreshAsync(); }
    });

    private async void DisableClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        if (Selected is { } item)
        {
            try { await service.DisableAsync(item.Id); }
            finally { await RefreshAsync(); }
        }
    });

    private async void UninstallClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        if (Selected is not { } item) return;
        ContentDialog confirm = new()
        {
            XamlRoot = XamlRoot, Title = SH.ViewPagePluginUninstallButton,
            Content = item.Id + "\n" + (SH.GetString("ViewPagePluginKeepData") ?? "Settings and data are retained."),
            PrimaryButtonText = SH.ViewPagePluginUninstallButton, CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close,
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
        try { await service.DisableAsync(item.Id, uninstall: true); }
        finally { await RefreshAsync(); }
    });

    private void SettingsClick(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } item || !service.IsActive(item.Id))
        {
            Show(InfoBarSeverity.Warning, SH.GetString("ViewPagePluginEnableFirst") ?? "Enable the plugin first.");
            return;
        }
        services.GetRequiredService<INavigationService>().Navigate<PluginSettingPage>(new NavigationExtraData(item.Id));
    }

    private async void UpdateClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        if (Selected is not { } item || PickPackage() is not { } path) return;
        VerifiedPackage verified = await Task.Run(() => PluginPackage.Verify(path));
        if (!await ConfirmTrustAsync(verified.Manifest, verified.Sha256)) return;
        await service.StageUpdateAsync(item.Id, path, verified);
        Show(InfoBarSeverity.Success, SH.GetString("ViewPagePluginPendingRestart") ?? "Update staged. Restart and apply it.");
        await RefreshAsync();
    });

    private async void ApplyClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        if (Selected is not { } item) return;
        await service.ApplyUpdateAsync(item.Id);
        await RefreshAsync();
    });

    private async void RollbackClick(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        if (Selected is not { } item) return;
        await service.RollbackAsync(item.Id);
        await RefreshAsync();
    });
}
