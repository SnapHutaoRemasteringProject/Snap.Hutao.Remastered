// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.UI.Input.HotKey;
using Snap.Hutao.Remastered.UI.Input.LowLevel;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[Service(ServiceLifetime.Scoped)]
public sealed partial class SettingHotKeyViewModel : Abstraction.ViewModel
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

    // Instance wrappers keep compiled UI bindings reactive without changing the
    // static settings access used by keyboard and WebView services.
    public int VideoSeekSeconds
    {
        get => WebView2VideoFastForwardOrRewindSeconds;
        set
        {
            if (VideoSeekSeconds == value) return;
            WebView2VideoFastForwardOrRewindSeconds = value;
            OnPropertyChanged(nameof(VideoSeekSeconds));
        }
    }

    public double InactiveWindowOpacity
    {
        get => CompactWebView2WindowInactiveOpacity;
        set
        {
            if (InactiveWindowOpacity == value) return;
            CompactWebView2WindowInactiveOpacity = value;
            OnPropertyChanged(nameof(InactiveWindowOpacity));
        }
    }

    public partial LowLevelKeyOptions LowLevelKeyOptions { get; }

    public partial RuntimeOptions RuntimeOptions { get; }

    public partial HotKeyOptions HotKeyOptions { get; }
}
