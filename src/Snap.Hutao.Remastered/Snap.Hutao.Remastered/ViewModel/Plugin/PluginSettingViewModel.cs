// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Plugin;

internal sealed record PluginSettingTarget(string Id, string Name);

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class PluginSettingViewModel : Abstraction.ViewModel
{
    private readonly PluginService pluginService;
    private readonly INavigationService navigationService;

    [GeneratedConstructor]
    public partial PluginSettingViewModel(IServiceProvider serviceProvider);

    internal PluginSettingTarget? Target { get; set; }

    public string PluginName => Target?.Name ?? string.Empty;

    [ObservableProperty]
    public partial List<PluginSettingItem> Settings { get; set; } = [];

    [ObservableProperty]
    public partial bool ActionsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; }

    protected override ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        if (Target is { } target && pluginService.IsActive(target.Id))
        {
            Settings = pluginService.Settings(target.Id).Select(setting => new PluginSettingItem(this, setting)).ToList();
        }
        else
        {
            Show(InfoBarSeverity.Warning, SH.ViewPagePluginEnableFirst);
        }

        return ValueTask.FromResult(true);
    }

    [Command("GoBackCommand")]
    private void GoBack() => navigationService.Navigate<PluginPage>(NavigationExtraData.Default, true);

    private void Show(InfoBarSeverity severity, string message)
    {
        if (CancellationToken.IsCancellationRequested) return;
        StatusSeverity = severity;
        StatusMessage = message;
        IsStatusOpen = true;
    }

    internal async Task SaveAsync(PluginSettingItem setting, bool reset)
    {
        if (!ActionsEnabled || CancellationToken.IsCancellationRequested || Target is not { } target) return;
        ActionsEnabled = false;
        try
        {
            if (reset)
            {
                await pluginService.ResetSettingAsync(target.Id, setting.Name);
                if (!CancellationToken.IsCancellationRequested) setting.ResetValue();
            }
            else
            {
                await pluginService.SetSettingAsync(target.Id, setting.Name, setting.GetValue());
            }

            Show(InfoBarSeverity.Success, reset ? SH.ViewPagePluginSettingResetSuccess : SH.ViewPagePluginSettingSaveSuccess);
        }
        catch (Exception error)
        {
            Show(InfoBarSeverity.Error, error.Message);
        }
        finally
        {
            if (!CancellationToken.IsCancellationRequested) ActionsEnabled = true;
        }
    }
}

public sealed partial class PluginSettingItem : ObservableObject
{
    private readonly object defaultValue;

    internal PluginSettingItem(PluginSettingViewModel viewModel, RegisteredPluginSetting setting)
    {
        Name = setting.Name;
        Description = setting.Description;
        defaultValue = setting.DefaultValue;
        SetValue(setting.Value);
        SaveCommand = new AsyncRelayCommand(() => viewModel.SaveAsync(this, reset: false));
        ResetCommand = new AsyncRelayCommand(() => viewModel.SaveAsync(this, reset: true));
    }

    public string Name { get; }
    public string Description { get; }
    public string DefaultText => Convert.ToString(defaultValue, CultureInfo.CurrentCulture) ?? string.Empty;
    public Visibility TextVisibility => defaultValue is string ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NumberVisibility => defaultValue is int ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FlagVisibility => defaultValue is bool ? Visibility.Visible : Visibility.Collapsed;
    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Number { get; set; }

    [ObservableProperty]
    public partial bool Flag { get; set; }

    internal object GetValue() => defaultValue switch
    {
        string => Text,
        bool => Flag,
        int when double.IsFinite(Number) && Number == Math.Truncate(Number) && Number >= int.MinValue && Number <= int.MaxValue => (int)Number,
        _ => throw HutaoException.InvalidOperation(SH.ViewPagePluginSettingInvalidInteger),
    };

    internal void ResetValue() => SetValue(defaultValue);

    private void SetValue(object value)
    {
        switch (value)
        {
            case string text: Text = text; break;
            case int number: Number = number; break;
            case bool flag: Flag = flag; break;
        }
    }
}
