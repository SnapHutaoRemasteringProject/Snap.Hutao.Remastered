// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Hutao.Plugin.Packaging;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Snap.Hutao.Remastered.ViewModel.Plugin;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class PluginViewModel : Abstraction.ViewModel
{
    private const int MaxIconSize = 4 * 1024 * 1024;

    private readonly IServiceProvider serviceProvider;
    private readonly PluginService pluginService;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IContentDialogQueue contentDialogQueue;
    private readonly IFileSystemPickerInteraction picker;
    private readonly INavigationService navigationService;
    private readonly ILogger<PluginViewModel> logger;
    private bool refreshing;
    private bool refreshRequested;

    [GeneratedConstructor]
    public partial PluginViewModel(IServiceProvider serviceProvider);

    [ObservableProperty]
    public partial List<PluginListItem> Plugins { get; set; } = [];

    [ObservableProperty]
    public partial bool ActionsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; }

    internal void AttachRoot(XamlRoot root) => pluginService.AttachRoot(root);

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        pluginService.Changed += OnPluginsChanged;
        await RunAsync(RefreshAsync);
        if (pluginService.RecoveryMode && !IsStatusOpen && !token.IsCancellationRequested)
        {
            Show(InfoBarSeverity.Warning, SH.ViewPagePluginRecovery);
        }
        return true;
    }

    protected override void UninitializeOverride() => pluginService.Changed -= OnPluginsChanged;

    private void OnPluginsChanged()
    {
        refreshRequested = true;
        if (ActionsEnabled) RunAsync(RefreshAsync).SafeForget();
    }

    private async Task RefreshAsync()
    {
        if (refreshing) return;
        refreshing = true;
        try
        {
            do
            {
                refreshRequested = false;
                InstalledPlugin[] entries = await pluginService.ScanAsync();
                var details = await Task.Run(() => entries.Select(ReadDetails).ToArray(), CancellationToken);
                List<PluginListItem> items = new(entries.Length);
                for (int i = 0; i < entries.Length; i++)
                {
                    if (CancellationToken.IsCancellationRequested) return;
                    InstalledPlugin entry = entries[i];
                    (PluginDiskState state, byte[]? icon, string? stateError) = details[i];
                    BitmapImage? image = await DecodeIconAsync(entry.Id, icon);
                    bool enabled = pluginService.IsActive(entry.Id);
                    items.Add(new PluginListItem(this, entry, state, image, enabled, stateError,
                        enabled ? pluginService.Commands(entry.Id) : []));
                }

                if (CancellationToken.IsCancellationRequested) return;
                Plugins = items;
            }
            while (refreshRequested);
        }
        finally
        {
            refreshing = false;
        }
    }

    private (PluginDiskState State, byte[]? Icon, string? Error) ReadDetails(InstalledPlugin entry)
    {
        byte[]? icon = null;
        try
        {
            if (entry.Manifest is { IconPath: { } path } manifest)
            {
                PackagePaths.RejectReparseAncestors(entry.Path);
                using FileStream package = File.OpenRead(entry.Path);
                if (package.Length > 256L * 1024 * 1024) throw new InvalidDataException("Plugin package size changed.");
                using ZipArchive archive = new(package, ZipArchiveMode.Read);
                ZipArchiveEntry? image = archive.Entries.FirstOrDefault(item => item.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));
                if (image is not { Length: > 0 and <= MaxIconSize })
                {
                    throw new InvalidDataException("Plugin icon is missing or has an invalid size.");
                }

                byte[] bytes = new byte[(int)image.Length];
                using Stream stream = image.Open();
                stream.ReadExactly(bytes);
                if (stream.ReadByte() != -1 || !Convert.ToHexStringLower(SHA256.HashData(bytes)).Equals(manifest.Files[path], StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Plugin icon no longer matches its verified manifest.");
                }

                icon = bytes;
            }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            logger.LogWarning(error, "Could not read the icon for plugin {PluginId}", entry.Id);
        }

        try { return (PluginPackageStore.ReadState(entry.Id), icon, null); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            return (new(), icon, error.Message);
        }
    }

    private async Task<BitmapImage?> DecodeIconAsync(string pluginId, byte[]? bytes)
    {
        if (bytes is null) return null;
        using MemoryStream stream = new(bytes, writable: false);
        using var randomAccessStream = stream.AsRandomAccessStream();
        BitmapImage image = new() { DecodePixelWidth = 96 };
        try
        {
            await image.SetSourceAsync(randomAccessStream);
            return image;
        }
        catch (Exception exception) when (exception is ArgumentException or System.Runtime.InteropServices.COMException)
        {
            logger.LogWarning(exception, "Could not decode the icon for plugin {PluginId}", pluginId);
            return null;
        }
    }

    private void Show(InfoBarSeverity severity, string message)
    {
        if (CancellationToken.IsCancellationRequested) return;
        StatusSeverity = severity;
        StatusMessage = message;
        IsStatusOpen = true;
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (!ActionsEnabled || CancellationToken.IsCancellationRequested) return;
        ActionsEnabled = false;
        try
        {
            await action();
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Plugin page operation cancelled.");
        }
        catch (Exception error)
        {
            Show(InfoBarSeverity.Error, error.Message);
        }
        finally
        {
            if (!CancellationToken.IsCancellationRequested) ActionsEnabled = true;
            if (refreshRequested && !CancellationToken.IsCancellationRequested)
            {
                refreshRequested = false;
                await RunAsync(RefreshAsync);
            }
        }
    }

    private string? PickPackage()
    {
        var result = picker.PickFile(SH.ServicePluginPickFileTitle, "HutaoPlugin", "*.hutao;*.hutaodisabled");
        return result.IsOk ? result.Value.ToString() : null;
    }

    private async Task<bool> ConfirmTrustAsync(PluginManifest manifest, string hash)
    {
        string content = $"{manifest.Name} ({manifest.Id}) {manifest.Version}\nSHA256: {hash}\n{string.Join(", ", manifest.Capabilities.Order(StringComparer.Ordinal))}\n\n{SH.ViewPagePluginTrustWarning}";
        if (Environment.IsPrivilegedProcess) content += "\n" + SH.ViewPagePluginElevatedWarning;
        if (manifest.GameLibraries.Count > 0)
        {
            content += "\n\n" + SH.ViewPagePluginGameLibrariesWarning + "\n" + string.Join("\n", manifest.GameLibraries);
        }

        return await ConfirmAsync(SH.ViewPagePluginTrustTitle, content, SH.ViewPagePluginTrustButton);
    }

    private async Task<bool> ConfirmAsync(string title, string content, string primaryText)
    {
        if (CancellationToken.IsCancellationRequested) return false;
        ContentDialog dialog = await contentDialogFactory.CreateInstanceAsync<ContentDialog>(serviceProvider);
        dialog.Title = title;
        dialog.Content = content;
        dialog.PrimaryButtonText = primaryText;
        dialog.CloseButtonText = SH.ContentDialogCancelCloseButtonText;
        dialog.DefaultButton = ContentDialogButton.Close;
        ContentDialogResult result = await contentDialogQueue.EnqueueAndShowAsync(dialog, CancellationToken).ShowTask;
        return result == ContentDialogResult.Primary && !CancellationToken.IsCancellationRequested;
    }

    [Command("InstallPluginCommand")]
    private Task InstallAsync() => RunAsync(async () =>
    {
        if (PickPackage() is not { } path) return;
        VerifiedPackage verified = await Task.Run(() => PluginPackage.Verify(path), CancellationToken);
        bool enable = Path.GetExtension(path).Equals(".hutao", StringComparison.OrdinalIgnoreCase);
        if (enable && !await ConfirmTrustAsync(verified.Manifest, verified.Sha256)) return;
        CancellationToken.ThrowIfCancellationRequested();
        await pluginService.InstallAsync(path, verified);
        if (enable) await pluginService.EnableAsync(verified.Manifest.Id, verified.Sha256);
    });

    [Command("RefreshPluginsCommand")]
    private Task RefreshPluginsAsync() => RunAsync(RefreshAsync);

    [Command("OpenPluginDirectoryCommand")]
    private Task OpenDirectoryAsync() => RunAsync(async () =>
    {
        await pluginService.ScanAsync();
        await Windows.System.Launcher.LaunchFolderAsync(await Windows.Storage.StorageFolder.GetFolderFromPathAsync(PluginPackageStore.Root));
    });

    internal Task EnableAsync(InstalledPlugin entry) => RunAsync(async () =>
    {
        if (entry is not { Manifest: { } manifest, Hash: { } hash }) throw HutaoException.InvalidOperation(entry.Error ?? SH.ViewPagePluginEnableFirst);
        if (!pluginService.IsActive(entry.Id) && await ConfirmTrustAsync(manifest, hash))
        {
            await pluginService.EnableAsync(entry.Id, hash);
        }
    });

    internal Task DisableAsync(InstalledPlugin entry) => RunAsync(() => pluginService.DisableAsync(entry.Id));

    internal Task UninstallAsync(InstalledPlugin entry) => RunAsync(async () =>
    {
        if (await ConfirmAsync(SH.ViewPagePluginUninstallButton, entry.Id + "\n" + SH.ViewPagePluginKeepData, SH.ViewPagePluginUninstallButton))
        {
            await pluginService.DisableAsync(entry.Id, uninstall: true);
        }
    });

    internal void OpenSettings(InstalledPlugin entry)
    {
        if (!ActionsEnabled || CancellationToken.IsCancellationRequested) return;
        if (pluginService.IsActive(entry.Id))
        {
            navigationService.Navigate<PluginSettingPage>(new NavigationExtraData(new PluginSettingTarget(entry.Id, entry.Manifest?.Name ?? entry.Id)));
        }
        else
        {
            Show(InfoBarSeverity.Warning, SH.ViewPagePluginEnableFirst);
        }
    }

    internal Task StageUpdateAsync(InstalledPlugin entry) => RunAsync(async () =>
    {
        if (PickPackage() is not { } path) return;
        VerifiedPackage verified = await Task.Run(() => PluginPackage.Verify(path), CancellationToken);
        if (!await ConfirmTrustAsync(verified.Manifest, verified.Sha256)) return;
        await pluginService.StageUpdateAsync(entry.Id, path, verified);
        Show(InfoBarSeverity.Success, SH.ViewPagePluginPendingRestart);
    });

    internal Task ApplyUpdateAsync(InstalledPlugin entry) => RunAsync(() => pluginService.ApplyUpdateAsync(entry.Id));
    internal Task RollbackAsync(InstalledPlugin entry) => RunAsync(() => pluginService.RollbackAsync(entry.Id));
    internal Task ExecuteAsync(InstalledPlugin entry, string command) => RunAsync(() => pluginService.ExecuteAsync(entry.Id, command));
}
