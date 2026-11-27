// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Hutao.Remastered.Service.Plugin;

namespace Snap.Hutao.Remastered.ViewModel.Plugin;

public sealed class PluginListItem
{
    internal PluginListItem(PluginViewModel viewModel, InstalledPlugin entry, PluginDiskState state, BitmapImage? icon, bool enabled, string? stateError, IReadOnlyList<CommandContribution> commands)
    {
        Name = entry.Manifest?.Name ?? entry.Id;
        Description = entry.IsLegacy ? SH.ViewPagePluginLegacyDescription : entry.Manifest?.Description ?? string.Empty;
        Version = entry.Manifest?.Version.ToString() ?? string.Empty;
        Author = entry.Manifest is { } manifest ? string.Join(", ", manifest.Author) : string.Empty;
        Icon = icon;
        IsEnabled = enabled;
        CanChangePackage = !entry.Conflict;
        CanEnable = CanChangePackage && entry.Manifest is not null && state.PendingHash is null;
        CanApply = CanChangePackage && state.PendingHash is not null && !enabled;
        CanRollback = CanChangePackage && (state.BackupHash is null ? state.PendingHash is not null : !enabled);
        Status = entry.IsLegacy
            ? SH.ViewPagePluginLegacyStatus
            : enabled ? SH.ViewPagePluginStatusEnabled : SH.ViewPagePluginStatusDisabled;
        List<string> details = [entry.Id];
        if (entry.Hash is { } hash) details.Add("SHA256: " + hash);
        if (state.PendingHash is { } pending) details.Add($"{SH.ViewPagePluginUpdateButton} SHA256: {pending}");
        if (state.BackupHash is { } backup) details.Add($"{SH.ViewPagePluginRollbackButton} SHA256: {backup}");
        if (!entry.IsLegacy && entry.Error is { } error) details.Add(error);
        if (state.LastError is { } lastError) details.Add(lastError);
        if (stateError is not null) details.Add(stateError);
        Details = string.Join("\n", details);

        EnableCommand = new AsyncRelayCommand(() => viewModel.EnableAsync(entry));
        DisableCommand = new AsyncRelayCommand(() => viewModel.DisableAsync(entry));
        UninstallCommand = new AsyncRelayCommand(() => viewModel.UninstallAsync(entry));
        OpenSettingsCommand = new RelayCommand(() => viewModel.OpenSettings(entry));
        UpdateCommand = new AsyncRelayCommand(() => viewModel.StageUpdateAsync(entry));
        ApplyUpdateCommand = new AsyncRelayCommand(() => viewModel.ApplyUpdateAsync(entry));
        RollbackCommand = new AsyncRelayCommand(() => viewModel.RollbackAsync(entry));
        Commands = commands.Select(command => (object)new PluginListCommand(command.Title,
            new AsyncRelayCommand(() => viewModel.ExecuteAsync(entry, command.Id)))).ToList();
    }

    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public string Author { get; }
    public string Status { get; }
    public string Details { get; }
    public BitmapImage? Icon { get; }
    public Visibility IconVisibility => Icon is null ? Visibility.Collapsed : Visibility.Visible;
    public Visibility PlaceholderVisibility => Icon is null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EnableVisibility => IsEnabled ? Visibility.Collapsed : Visibility.Visible;
    public Visibility DisableVisibility => IsEnabled ? Visibility.Visible : Visibility.Collapsed;
    public bool IsEnabled { get; }
    public bool CanChangePackage { get; }
    public bool CanEnable { get; }
    public bool CanApply { get; }
    public bool CanRollback { get; }
    public ICommand EnableCommand { get; }
    public ICommand DisableCommand { get; }
    public ICommand UninstallCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand UpdateCommand { get; }
    public ICommand ApplyUpdateCommand { get; }
    public ICommand RollbackCommand { get; }
    public List<object> Commands { get; }
}

public sealed record PluginListCommand(string Title, ICommand Command);
