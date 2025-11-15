// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Core.Property;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Model;
using Snap.Hutao.Service.Abstraction;
using Snap.Hutao.Service.BackgroundImage;
using Snap.Hutao.UI.Xaml.Media.Backdrop;
using Snap.Hutao.Web.Bridge;
using Snap.Hutao.Web.Hoyolab;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Snap.Hutao.Service;

[Service(ServiceLifetime.Singleton)]
internal sealed partial class AppOptions : DbStoreOptions
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

    [field: MaybeNull]
    public IObservableProperty<TimeSpan> CalendarServerTimeZoneOffset { get => field ??= CreatePropertyForStructUsingCustom(SettingKeys.CalendarServerTimeZoneOffset, ServerRegionTimeZone.CommonOffset, TimeSpan.Parse, static v => v.ToString()); }

    [field: MaybeNull]
    public IObservableProperty<LastWindowCloseBehavior> LastWindowCloseBehavior { get => field ??= CreateProperty(SettingKeys.LastWindowCloseBehavior, Service.LastWindowCloseBehavior.EnsureNotifyIconCreated); }
}