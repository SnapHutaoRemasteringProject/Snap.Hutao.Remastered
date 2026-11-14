// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Microsoft.Windows.AppNotifications;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.IO.Hashing;
using Snap.Hutao.Remastered.Core.Setting;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Snap.Hutao.Remastered.Core;

public static class HutaoRuntime
{
    public static Version Version { get; } = RuntimeEnvironment.IsPackaged ? Package.Current.Id.Version.ToVersion() : AppVersion.CurrentVersion;

    public static string UserAgent { get; } = $"Snap Hutao/{Version}";

    public static string DataDirectory { get; } = InitializeDataDirectory();

    public static string LocalCacheDirectory { get; } = RuntimeEnvironment.IsPackaged
        ? ApplicationData.Current.LocalCacheFolder.Path
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"SnapHutaoRemastered\Cache");

    /// <summary>
    /// 对应 <c>ms-appdata:///local/</c> 的本地数据目录。
    /// 磁贴通知只支持 <c>ms-appx:///</c>、<c>ms-appdata:///local/</c> 与 http(s):// 三种图片地址，
    /// 没有 localcache 协议，需要被磁贴引用的文件必须放在这里。
    /// </summary>
    public static string LocalFolderDirectory { get; } = RuntimeEnvironment.IsPackaged
        ? ApplicationData.Current.LocalFolder.Path
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"SnapHutaoRemastered\LocalFolder");

    public static string FamilyName { get; } = RuntimeEnvironment.IsPackaged
        ? Package.Current.Id.FamilyName
        : "Snap.Hutao.Remastered";

    public static string DeviceId { get; } = InitializeDeviceId();

    public static WebView2Version WebView2Version { get; } = InitializeWebView2();

    public static bool IsProcessElevated { get; } = LocalSetting.Get(SettingKeys.OverrideElevationRequirement, false) || Environment.IsPrivilegedProcess;

    // Requires main thread
    public static bool IsAppNotificationEnabled { get; } = AppNotificationManager.Default.Setting is AppNotificationSetting.Enabled or AppNotificationSetting.Unsupported;

    public static string? GetDisplayName()
    {
        // AppNameAndVersion
        // AppDevNameAndVersion
        // AppElevatedNameAndVersion
        // AppElevatedDevNameAndVersion
        string name = new StringBuilder()
            .Append("App")
            .AppendIf(IsProcessElevated, "Elevated")
#if DEBUG
            .Append("Dev")
#endif
            .Append("NameAndVersion")
            .ToString();

        Debug.Assert(XamlApplicationLifetime.CultureInfoInitialized);
        string? displayName = SH.GetString(name, Version);
        return displayName is null ? null : string.Intern(displayName);
    }

    public static string? GetDisplayNameForNotifyIcon()
    {
        // AppName
        // AppDevName
        string name = new StringBuilder()
            .Append("App")
#if DEBUG
            .Append("Dev")
#endif
            .Append("Name")
            .ToString();

        Debug.Assert(XamlApplicationLifetime.CultureInfoInitialized);
        string? displayName = SH.GetString(name);
        return displayName is null ? null : string.Intern(displayName);
    }

    public static ValueFile GetDataDirectoryFile(string fileName)
    {
        return string.Intern(Path.Combine(DataDirectory, fileName));
    }

    public static ValueFile GetDataUpdateCacheDirectoryFile(string fileName)
    {
        string directory = Path.Combine(DataDirectory, "UpdateCache");
        Directory.CreateDirectory(directory);
        return string.Intern(Path.Combine(directory, fileName));
    }

    public static ValueDirectory GetDataServerCacheDirectory()
    {
        string directory = Path.Combine(DataDirectory, "ServerCache");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetDataBackgroundDirectory()
    {
        string directory = Path.Combine(DataDirectory, "Background");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetDataScreenshotDirectory()
    {
        string directory = Path.Combine(DataDirectory, "Screenshot");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetDataRepositoryDirectory()
    {
        string directory = Path.Combine(DataDirectory, "Repository");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetLocalCacheImageCacheDirectory()
    {
        string directory = Path.Combine(LocalCacheDirectory, "ImageCache");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static string GetWebView2UserDataDirectory()
    {
        string directory = Path.Combine(DataDirectory, "WebView2");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string InitializeDataDirectory()
    {
        // Delete the previous data folder if it exists
        try
        {
            string previousDirectory = LocalSetting.Get(SettingKeys.PreviousDataDirectoryToDelete, string.Empty);
            if (!string.IsNullOrEmpty(previousDirectory) && Directory.Exists(previousDirectory))
            {
                Directory.SetReadOnly(previousDirectory, false);
                Directory.Delete(previousDirectory, true);
            }
        }
        finally
        {
            LocalSetting.Set(SettingKeys.PreviousDataDirectoryToDelete, string.Empty);
        }

        // Check if the preferred path is set
        string currentDirectory = LocalSetting.Get(SettingKeys.DataDirectory, string.Empty);

        if (!string.IsNullOrEmpty(currentDirectory))
        {
            Directory.CreateDirectory(currentDirectory);
            return currentDirectory;
        }

        const string FolderName
#if IS_ALPHA_BUILD
        = "HutaoAlpha";
#elif IS_CANARY_BUILD
        = "HutaoCanary";
#else
        = "Hutao";
#endif

        // Check if the old documents path exists
        string myDocumentsHutaoDirectory = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FolderName));
        if (Directory.Exists(myDocumentsHutaoDirectory))
        {
            LocalSetting.Set(SettingKeys.DataDirectory, myDocumentsHutaoDirectory);
            return myDocumentsHutaoDirectory;
        }

        // Prefer LocalApplicationData
        string localApplicationData = RuntimeEnvironment.IsPackaged
            ? ApplicationData.Current.LocalFolder.Path
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string finalFolderName = RuntimeEnvironment.IsPackaged ? FolderName : "SnapHutaoRemastered";
        string path = Path.GetFullPath(Path.Combine(localApplicationData, finalFolderName));
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex)
        {
            // FileNotFoundException | UnauthorizedAccessException
            // We don't have enough permission
            HutaoException.InvalidOperation($"Failed to create data folder: {path}", ex);
        }

        LocalSetting.Set(SettingKeys.DataDirectory, path);
        return path;
    }

    private static string InitializeDeviceId()
    {
        string userName = Environment.UserName;
        object? machineGuid = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography\", "MachineGuid", userName);
        return Hash.ToHexString(HashAlgorithmName.MD5, $"{userName}{machineGuid}");
    }

    private static WebView2Version InitializeWebView2()
    {
        try
        {
            string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return new(version, version, true);
        }
        catch (FileNotFoundException)
        {
            return new(string.Empty, SH.CoreWebView2HelperVersionUndetected, false);
        }
    }
}