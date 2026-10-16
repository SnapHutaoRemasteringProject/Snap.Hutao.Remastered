// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.GachaLog.QueryProvider;
using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Service.Notification;
using System.IO;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[Service(ServiceLifetime.Scoped)]
public sealed partial class SettingGameViewModel : Abstraction.ViewModel
{
    private readonly LaunchOptions launchOptions;
    private readonly IMessenger messenger;

    [GeneratedConstructor]
    public partial SettingGameViewModel(IServiceProvider serviceProvider);

    public partial AppOptions AppOptions { get; }

    public bool ForceUsingTouchScreenWhenIntegratedTouchPresent
    {
        get => LocalSetting.Get(SettingKeys.LaunchForceUsingTouchScreen, false);
        set
        {
            if (ForceUsingTouchScreenWhenIntegratedTouchPresent == value) return;
            LocalSetting.Set(SettingKeys.LaunchForceUsingTouchScreen, value);
            OnPropertyChanged(nameof(ForceUsingTouchScreenWhenIntegratedTouchPresent));
        }
    }
    public bool CloseWhenGameStarted
    {
        get => LocalSetting.Get(SettingKeys.LaunchIsCloseWhenGameStarted, false);
        set
        {
            if (CloseWhenGameStarted == value) return;
            LocalSetting.Set(SettingKeys.LaunchIsCloseWhenGameStarted, value);
            OnPropertyChanged(nameof(CloseWhenGameStarted));
        }
    }

    public bool AutoSignInEnabled
    {
        get => LocalSetting.Get(SettingKeys.AutoSignInEnabled, true);
        set
        {
            if (AutoSignInEnabled == value) return;
            LocalSetting.Set(SettingKeys.AutoSignInEnabled, value);
            OnPropertyChanged(nameof(AutoSignInEnabled));
        }
    }

    [Command("DeleteGameWebCacheCommand")]
    private void DeleteGameWebCache()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Delete game web cache", "SettingGameViewModel.Command"));

        string? gamePath = launchOptions.GamePathEntry.Value?.Path;

        if (string.IsNullOrEmpty(gamePath))
        {
            // TODO: show message
            return;
        }

        string cacheFilePath = GachaLogQueryWebCacheProvider.GetCacheFile(gamePath);
        string? cacheFolder = Path.GetDirectoryName(cacheFilePath);

        if (!Directory.Exists(cacheFolder))
        {
            messenger.Send(InfoBarMessage.Warning(SH.FormatViewModelSettingClearWebCachePathInvalid(cacheFolder)));
            return;
        }

        try
        {
            Directory.Delete(cacheFolder, true);
            messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingClearWebCacheSuccess));
        }
        catch (UnauthorizedAccessException)
        {
            messenger.Send(InfoBarMessage.Warning(SH.ViewModelSettingClearWebCacheFail));
        }
    }
}
