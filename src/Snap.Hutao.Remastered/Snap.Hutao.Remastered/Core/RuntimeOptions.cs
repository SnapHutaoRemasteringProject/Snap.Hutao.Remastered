// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core;

/// <summary>
/// Exposes runtime values to XAML bindings, including generated compiled bindings.
/// </summary>
[Service(ServiceLifetime.Singleton)]
public sealed class RuntimeOptions
{
    public Version Version { get => HutaoRuntime.Version; }

    public string DataFolder { get => HutaoRuntime.DataDirectory; }

    public string DeviceId { get => HutaoRuntime.DeviceId; }

    public string WebView2Version { get => HutaoRuntime.WebView2Version.Version; }

    public bool IsToastAvailable { get => HutaoRuntime.IsAppNotificationEnabled; }
}
