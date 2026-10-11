// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.API.Model;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using System.Collections.ObjectModel;
using System.IO;

namespace Snap.Hutao.Remastered.ViewModel.Plugin;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class PluginViewModel : Abstraction.ViewModel
{
    private readonly IPluginService pluginService;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly ITaskContext taskContext;
    private readonly IFileSystemPickerInteraction fileSystem;
    private readonly IMessenger messenger;
    private readonly INavigationService navigationService;

    [GeneratedConstructor]
    public partial PluginViewModel(IServiceProvider serviceProvider);

    [ObservableProperty]
    public partial ObservableCollection<PluginInfo> Plugins { get; set; } = new();

    [ObservableProperty]
    public partial PluginInfo? SelectedPlugin { get; set; }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        await taskContext.SwitchToMainThreadAsync();

        // Plugins are loaded during background initialization, so this page can be open before any of
        // them exist. The subscription is the only thing that makes them appear once they finish loading.
        pluginService.PluginsChanged -= OnPluginsChanged;
        pluginService.PluginsChanged += OnPluginsChanged;

        RefreshPluginList();
        return true;
    }

    protected override void UninitializeOverride()
    {
        // PluginService is a singleton, so a subscription left behind would keep this scoped view alive.
        pluginService.PluginsChanged -= OnPluginsChanged;
    }

    private void OnPluginsChanged(object? sender, EventArgs args)
    {
        // Raised on the main thread by PluginService.
        RefreshPluginList();
    }

    private void RefreshPluginList()
    {
        Plugins.Clear();

        foreach (PluginInfo plugin in pluginService.GetAllPluginInfos())
        {
            Plugins.Add(plugin);
        }
    }

    [Command("InstallPluginCommand")]
    private async Task InstallPluginAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Install plugin", "PluginViewModel.Command"));

        try
        {
            ValueResult<bool, ValueFile> file = fileSystem.PickFile(SH.ServicePluginPickFileTitle, "HutaoPlugin", "*.hutao");
            if (!file.IsOk)
            {
                return;
            }

            string path = file.Value.ToString();
            await pluginService.InstallPluginAsync(path);
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallFailed, ex));
        }
    }

    [Command("EnablePluginCommand")]
    private async Task EnablePluginAsync(PluginInfo? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Enable plugin", "PluginViewModel.Command", new Dictionary<string, string>
        {
            { "PluginId", plugin.Id },
            { "PluginName", plugin.Manifest.Name },
        }));

        await pluginService.EnablePluginAsync(plugin.Id);
    }

    [Command("DisablePluginCommand")]
    private async Task DisablePluginAsync(PluginInfo? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Disable plugin", "PluginViewModel.Command", new Dictionary<string, string>
        {
            { "PluginId", plugin.Id },
            { "PluginName", plugin.Manifest.Name },
        }));

        await pluginService.DisablePluginAsync(plugin.Id);
    }

    [Command("UninstallPluginCommand")]
    private async Task UninstallPluginAsync(PluginInfo? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Uninstall plugin", "PluginViewModel.Command", new Dictionary<string, string>
        {
            { "PluginId", plugin.Id },
            { "PluginName", plugin.Manifest.Name },
        }));

        ContentDialogResult result = await contentDialogFactory
            .CreateForConfirmCancelAsync(
                SH.FormatServicePluginUninstallTitle(plugin.Manifest.Name),
                SH.FormatServicePluginUninstallDescription(plugin.Manifest.Name))
            .ConfigureAwait(false);

        if (result is ContentDialogResult.Primary)
        {
            pluginService.UninstallPlugin(plugin.Id);
        }
    }

    [Command("RefreshPluginsCommand")]
    private async Task RefreshPluginsAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Refresh plugins", "PluginViewModel.Command"));

        // The list is kept current by the PluginsChanged subscription; only a page that was opened
        // before the startup scan finished would need this, and that case is already covered.
        RefreshPluginList();
    }

    [Command("OpenPluginDirectoryCommand")]
    private void OpenPluginDirectory()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Open plugin directory", "PluginViewModel.Command"));

        string pluginsDirectory = Path.Combine(HutaoRuntime.DataDirectory, "Plugins");
        if (!Directory.Exists(pluginsDirectory))
        {
            Directory.CreateDirectory(pluginsDirectory);
        }

        System.Diagnostics.Process.Start("explorer.exe", pluginsDirectory);
    }

    [Command("OpenPluginSettingsCommand")]
    private async Task OpenPluginSettingsAsync(PluginInfo? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Open plugin settings", "PluginViewModel.Command", new Dictionary<string, string>
        {
            { "PluginId", plugin.Id },
            { "PluginName", plugin.Manifest.Name },
        }));

        await navigationService.NavigateAsync<PluginSettingPage>(new NavigationExtraData(plugin.Id));
    }
}
