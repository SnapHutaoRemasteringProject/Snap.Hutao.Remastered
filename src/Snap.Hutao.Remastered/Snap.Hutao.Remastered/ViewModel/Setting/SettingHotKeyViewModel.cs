// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.UI.Input.HotKey;
using Snap.Hutao.Remastered.UI.Input.LowLevel;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[Service(ServiceLifetime.Scoped)]
internal sealed partial class SettingHotKeyViewModel : Abstraction.ViewModel
{
    [GeneratedConstructor]
    public partial SettingHotKeyViewModel(IServiceProvider serviceProvider);

    public static int WebView2VideoFastForwardOrRewindSeconds
    {
        get => LocalSetting.Get(SettingKeys.WebView2VideoFastForwardOrRewindSeconds, 5);
        set => LocalSetting.Set(SettingKeys.WebView2VideoFastForwardOrRewindSeconds, value);
    }

    public static double CompactWebView2WindowInactiveOpacity
    {
        get => LocalSetting.Get(SettingKeys.CompactWebView2WindowInactiveOpacity, 50D);
        set => LocalSetting.Set(SettingKeys.CompactWebView2WindowInactiveOpacity, value);
    }

    public partial LowLevelKeyOptions LowLevelKeyOptions { get; }

    public partial RuntimeOptions RuntimeOptions { get; }

    public partial HotKeyOptions HotKeyOptions { get; }
}