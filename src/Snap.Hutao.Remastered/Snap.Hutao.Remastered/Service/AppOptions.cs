// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Abstraction;
using Snap.Hutao.Remastered.Service.BackgroundImage;
using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.UI.Xaml.Media.Backdrop;
using Snap.Hutao.Remastered.Web.Bridge;
using Snap.Hutao.Remastered.Web.Hoyolab;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Snap.Hutao.Remastered.Service;

[Service(ServiceLifetime.Singleton)]
public sealed partial class AppOptions : DbStoreOptions
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial AppOptions(IServiceProvider serviceProvider);

    public static bool NotifyIconCreated { get => XamlApplicationLifetime.NotifyIconCreated; }

    public Lazy<ImmutableArray<NameValue<ElementTheme>>> LazyElementThemes { get; } = new(static () =>
    [
        new(SH.CoreWindowThemeLight, Microsoft.UI.Xaml.ElementTheme.Light),
        new(SH.CoreWindowThemeDark, Microsoft.UI.Xaml.ElementTheme.Dark),
        new(SH.CoreWindowThemeSystem, Microsoft.UI.Xaml.ElementTheme.Default),
    ]);

    public Lazy<ImmutableArray<NameValue<Region>>> LazyRegions { get; } = new(static () =>
    {
        Debug.Assert(XamlApplicationLifetime.CultureInfoInitialized);
        return KnownRegions.Value;
    });

    public Lazy<ImmutableArray<NameValue<TimeSpan>>> LazyCalendarServerTimeZoneOffsets { get; } = new(static () =>
    {
        Debug.Assert(XamlApplicationLifetime.CultureInfoInitialized);
        return KnownServerRegionTimeZones.Value;
    });

    public ImmutableArray<NameValue<BackdropType>> BackdropTypes { get; } = ImmutableCollectionsNameValue.FromEnum<BackdropType>(type => type >= 0);

    public ImmutableArray<NameValue<BackgroundImageType>> BackgroundImageTypes { get; } = ImmutableCollectionsNameValue.FromEnum<BackgroundImageType>(type => type.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture) ?? string.Empty);

    public ImmutableArray<NameValue<BridgeShareSaveType>> BridgeShareSaveTypes { get; } = ImmutableCollectionsNameValue.FromEnum<BridgeShareSaveType>(type => type.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture) ?? string.Empty);

    public ImmutableArray<NameValue<LastWindowCloseBehavior>> LastWindowCloseBehaviors { get; } = ImmutableCollectionsNameValue.FromEnum<LastWindowCloseBehavior>(static @enum => @enum.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture) ?? string.Empty);

    [field: MaybeNull]
    public IObservableProperty<bool> IsEmptyHistoryWishVisible { get => field ??= CreateProperty(SettingKeys.IsEmptyHistoryWishVisible, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsUnobtainedWishItemVisible { get => field ??= CreateProperty(SettingKeys.IsUnobtainedWishItemVisible, false); }

    [field: MaybeNull]
    public IObservableProperty<BackdropType> BackdropType { get => field ??= CreateProperty(SettingKeys.SystemBackdropType, UI.Xaml.Media.Backdrop.BackdropType.Mica); }

    [field: MaybeNull]
    public IObservableProperty<ElementTheme> ElementTheme { get => field ??= CreateProperty(SettingKeys.ElementTheme, Microsoft.UI.Xaml.ElementTheme.Default); }

    [field: MaybeNull]
    public IObservableProperty<BackgroundImageType> BackgroundImageType { get => field ??= CreateProperty(SettingKeys.BackgroundImageType, BackgroundImage.BackgroundImageType.None); }

    [field: MaybeNull]
    public IObservableProperty<Region> Region { get => field ??= CreatePropertyForStructUsingCustom(SettingKeys.AnnouncementRegion, Web.Hoyolab.Region.CNGF01, Web.Hoyolab.Region.FromRegionString, Web.Hoyolab.Region.ToRegionString); }

    [field: MaybeNull]
    public IObservableProperty<string> GeetestCustomCompositeUrl { get => field ??= CreateProperty(SettingKeys.GeetestCustomCompositeUrl, string.Empty); }

    [field: MaybeNull]
    public IObservableProperty<int> DownloadSpeedLimitPerSecondInKiloByte { get => field ??= CreateProperty(SettingKeys.DownloadSpeedLimitPerSecondInKiloByte, 0); }

    [field: MaybeNull]
    public IObservableProperty<BridgeShareSaveType> BridgeShareSaveType { get => field ??= CreateProperty(SettingKeys.BridgeShareSaveType, Web.Bridge.BridgeShareSaveType.CopyToClipboard); }

    /// <summary>
    /// 是否手动设置覆盖了自动测试选择仓库的逻辑
    /// Whether the logic of automatically selecting the repository based on testing is overridden by manual setting
    /// </summary>
    [field: MaybeNull]
    public IObservableProperty<string> GitRepositoryDomainOverride { get => field ??= CreateProperty(SettingKeys.GitRepositoryDomainOverride, GitRepositoryDomainSetting.Auto); }

    /// <summary>
    /// 最后一次成功测试镜像源的时间（UTC）
    /// Last time when mirror sources were successfully tested (UTC)
    /// </summary>
    [field: MaybeNull]
    public IObservableProperty<string> GitMirrorLastTestTimeUtc { get => field ??= CreateProperty(SettingKeys.GitMirrorLastTestTimeUtc, string.Empty); }

    /// <summary>
    /// 最后一次获取的镜像源哈希值，用于检测源列表是否改变
    /// Hash of last fetched mirror sources to detect if the list has changed
    /// </summary>
    [field: MaybeNull]
    public IObservableProperty<string> GitMirrorSourcesHash { get => field ??= CreateProperty(SettingKeys.GitMirrorSourcesHash, string.Empty); }

    /// <summary>
    /// 上次测速时间（UTC），用于判断是否需要重新测速
    /// </summary>
    [field: MaybeNull]
    public IObservableProperty<string> GitMirrorSpeedTestLastRun { get => field ??= CreateProperty(SettingKeys.GitMirrorSpeedTestLastRun, string.Empty); }

    /// <summary>
    /// 距上次仓库测速相隔的日期（日）
    /// The interval days between the last repos speed test.
    /// </summary>
    [field: MaybeNull]
    public IObservableProperty<int> GitMirrorSpeedTestIntervalDays { get => field ??= CreateProperty(SettingKeys.GitMirrorSpeedTestIntervalDays, 30); }

    [field: MaybeNull]
    public IObservableProperty<TimeSpan> CalendarServerTimeZoneOffset { get => field ??= CreatePropertyForStructUsingCustom(SettingKeys.CalendarServerTimeZoneOffset, ServerRegionTimeZone.CommonOffset, TimeSpan.Parse, static v => v.ToString()); }

    [field: MaybeNull]
    public IObservableProperty<LastWindowCloseBehavior> LastWindowCloseBehavior { get => field ??= CreateProperty(SettingKeys.LastWindowCloseBehavior, Service.LastWindowCloseBehavior.EnsureNotifyIconCreated); }

    [field: MaybeNull]
    public IObservableProperty<bool> AutoRestartAsAdmin { get => field ??= CreateProperty(SettingKeys.AutoRestartAsAdmin, false).WithValueChangedCallback(OnAutoRestartAsAdminChanged, this); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsStartupEnabled { get => field ??= CreateProperty(SettingKeys.StartupEnabled, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsStartupAsAdminEnabled { get => field ??= CreateProperty(SettingKeys.StartupAsAdminEnabled, false); }

    private static void OnAutoRestartAsAdminChanged(bool isAdminRestart, AppOptions appOptions)
    {
        // When AutoRestartAsAdmin is set to True, automatically set IsStartupAsAdminEnabled to True
        if (isAdminRestart && !appOptions.IsStartupAsAdminEnabled.Value)
        {
            appOptions.IsStartupAsAdminEnabled.Value = true;
        }
        // When AutoRestartAsAdmin is set to False, do NOT automatically change IsStartupAsAdminEnabled
    }

}
