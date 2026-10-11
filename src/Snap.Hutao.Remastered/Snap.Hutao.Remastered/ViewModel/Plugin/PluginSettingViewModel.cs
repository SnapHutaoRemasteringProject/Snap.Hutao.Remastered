// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.API.Model.Plugin;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.ViewModel.Plugin;

[Service(ServiceLifetime.Scoped)]
public sealed partial class PluginSettingViewModel : Abstraction.ViewModel
{
    private readonly IPluginSettingService pluginSettingService;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly INavigationService navigationService;

    [ObservableProperty]
    public partial string PluginId { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string PluginName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<EditableSettingItem> Settings { get; set; } = new();

    [GeneratedConstructor]
    public partial PluginSettingViewModel(IServiceProvider serviceProvider);

    [Command("GoBackCommand")]
    private void GoBack()
    {
        navigationService.Navigate<PluginPage>(NavigationExtraData.Default);
    }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        await taskContext.SwitchToMainThreadAsync();

        if (string.IsNullOrEmpty(PluginId))
        {
            return false;
        }

        try
        {
            IReadOnlyList<RegisteredSettingInfo> registeredSettings = pluginSettingService.GetRegisteredSettings(PluginId);
            List<EditableSettingItem> settingItems = new List<EditableSettingItem>();

            foreach (RegisteredSettingInfo settingInfo in registeredSettings)
            {
                object? currentValue = await pluginSettingService.GetSettingAsync(
                    settingInfo.ValueType,
                    PluginId, 
                    settingInfo.Name, 
                    settingInfo.DefaultValue);

                EditableSettingItem item = new EditableSettingItem
                {
                    PluginId = PluginId,
                    Name = settingInfo.Name,
                    Description = settingInfo.Description ?? string.Empty,
                    ValueType = settingInfo.ValueType,
                    DefaultValue = settingInfo.DefaultValue,
                    CurrentValue = currentValue,
                    CanEdit = true
                };

                settingItems.Add(item);
            }

            Settings = new ObservableCollection<EditableSettingItem>(settingItems);
            return true;
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.ViewPagePluginSettingLoadFailed, ex));
            return false;
        }
    }

    [Command("SaveSettingCommand")]
    private async Task SaveSettingAsync(EditableSettingItem? settingItem)
    {
        if (settingItem == null || string.IsNullOrEmpty(PluginId))
        {
            return;
        }

        try
        {
            await pluginSettingService.SetSettingAsync(
                settingItem.ValueType,
                PluginId, 
                settingItem.Name,
                ConvertToCorrectType(settingItem)
                );

            messenger.Send(InfoBarMessage.Success(SH.FormatViewPagePluginSettingSaveSuccess(settingItem.Name)));
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatViewPagePluginSettingSaveFailed(settingItem.Name), ex));
        }
    }

    [Command("ResetSettingCommand")]
    private async Task ResetSettingAsync(EditableSettingItem? settingItem)
    {
        if (settingItem == null || string.IsNullOrEmpty(PluginId))
        {
            return;
        }

        try
        {
            settingItem.CurrentValue = settingItem.DefaultValue;
            await pluginSettingService.SetSettingAsync(
                settingItem.ValueType,
                PluginId,
                settingItem.Name,
                ConvertToCorrectType(settingItem)
                );

            messenger.Send(InfoBarMessage.Success(SH.FormatViewPagePluginSettingResetSuccess(settingItem.Name)));
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatViewPagePluginSettingResetFailed(settingItem.Name), ex));
        }
    }

    private static object? ConvertToCorrectType(EditableSettingItem settingItem)
    {
        string? s = settingItem.CurrentValue?.ToString();

        if (s == null)
        {
            return null;
        }

        if (settingItem.ValueType == typeof(int))
        {
            if (int.TryParse(s, out int i))
            {
                return i;
            }
        }
        else if (settingItem.ValueType == typeof(string))
        {
            return s;
        }
        else if (settingItem.ValueType == typeof(bool))
        {
            if (bool.TryParse(s, out bool b))
            {
                return b;
            }
        }

        return null;
    }
}
