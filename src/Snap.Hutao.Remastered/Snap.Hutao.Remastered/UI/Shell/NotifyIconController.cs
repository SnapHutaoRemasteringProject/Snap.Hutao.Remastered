// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Xaml.View.Window;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Windows.ApplicationModel;

namespace Snap.Hutao.Remastered.UI.Shell;

[Service(ServiceLifetime.Singleton)]
public sealed partial class NotifyIconController : IDisposable
{
    private static bool constructed;

    private readonly Lock syncRoot = new();

    private readonly ICurrentXamlWindowReference currentXamlWindowReference;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly LazySlim<NotifyIconContextMenu> lazyMenu;
    private readonly NotifyIconXamlHostWindow xamlHostWindow;
    private readonly IServiceProvider serviceProvider;
    private readonly HutaoNativeNotifyIcon native;
    private GCHandle<NotifyIconController> handle;

    private bool disposed;

    public NotifyIconController(IServiceProvider serviceProvider)
    {
        if (Interlocked.Exchange(ref constructed, true))
        {
            // Actively prevent multiple constructions, if this happens, it's definitely a bug.
            // For example: the below part of the ctor throws an exception.
            throw new InvalidOperationException("NotifyIconController is already constructed.");
        }

        currentXamlWindowReference = serviceProvider.GetRequiredService<ICurrentXamlWindowReference>();
        contentDialogFactory = serviceProvider.GetRequiredService<IContentDialogFactory>();
        this.serviceProvider = serviceProvider;
        lazyMenu = new(() => new(serviceProvider));

        Guid id = MemoryMarshal.AsRef<Guid>(MD5.HashData(Encoding.UTF8.GetBytes(HutaoRuntime.GetDisplayNameForNotifyIcon() ?? "Snap Hutao")).AsSpan());
        native = HutaoNative.Instance.MakeNotifyIcon(InstalledLocation.GetAbsolutePath("Assets/Logo.ico"), in id);

        xamlHostWindow = new(serviceProvider);
        xamlHostWindow.MoveAndResize(default);

        handle = new(this);
    }

    public static Lock InitializationSyncRoot { get; } = new();

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (syncRoot)
        {
            disposed = true;
            try
            {
                native.Destroy();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }

            handle.Dispose();
        }
    }

    public unsafe void Create()
    {
        native.Create(HutaoNativeNotifyIconCallback.Create(&OnNotifyIconCallback), handle, HutaoRuntime.GetDisplayNameForNotifyIcon() ??"Snap Hutao Remastered");
        if (XamlApplicationLifetime.IsFirstRunAfterUpdate)
        {
            CleanupLegacyMsixNotifyIconRegistryEntries();
        }
    }

    public bool IsPromoted()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        try
        {
            return native.IsPromoted;
        }
        catch (Exception ex)
        {
            // If the lpValue registry value does not exist, the function returns ERROR_FILE_NOT_FOUND
            if (ex is not (FileNotFoundException or COMException or ObjectDisposedException))
            {
                SentrySdk.CaptureException(ex);
            }

            return false;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnNotifyIconCallback(HutaoNativeNotifyIconCallbackKind kind, RECT icon, POINT point, GCHandle<NotifyIconController> data)
    {
        if (data.Target is not { } controller)
        {
            return;
        }

        switch (kind)
        {
            case HutaoNativeNotifyIconCallbackKind.TaskbarCreated:
                controller.OnRecreateNotifyIconRequested();
                break;
            case HutaoNativeNotifyIconCallbackKind.ContextMenu:
                controller.OnContextMenuRequested(icon, point);
                break;
            case HutaoNativeNotifyIconCallbackKind.LeftButtonDown:
                controller.OnContextMenuRequested(icon, point);
                break;
            case HutaoNativeNotifyIconCallbackKind.LeftButtonDoubleClick:
                controller.OnWindowRequested();
                break;
        }
    }

    private void OnRecreateNotifyIconRequested()
    {
        if (disposed || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        native.Recreate("Snap Hutao Remastered");
    }

    private void OnContextMenuRequested(RECT icon, POINT point)
    {
        if (disposed)
        {
            return;
        }

        if (XamlApplicationLifetime.Exiting)
        {
            Debugger.Break();
            return;
        }

        // https://github.com/DGP-Studio/Snap.Hutao.Remastered/issues/2434
        // Now we disable the context menu when the dialog is showing.
        if (contentDialogFactory.IsDialogShowing)
        {
            return;
        }

        xamlHostWindow.ShowFlyoutAt(lazyMenu.Value, new(point.x, point.y), icon);
    }

    private void OnWindowRequested()
    {
        if (disposed)
        {
            return;
        }

        if (XamlApplicationLifetime.Exiting)
        {
            Debugger.Break();
            return;
        }

        switch (currentXamlWindowReference.Window)
        {
            case null:
                {
                    // MainWindow is closed, show it
                    MainWindow mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                    currentXamlWindowReference.Window = mainWindow;
                    mainWindow.SwitchTo();
                    mainWindow.AppWindow.MoveInZOrderAtTop();
                    return;
                }

            default:
                {
                    Window window = currentXamlWindowReference.Window;

                    // While window is closing, currentXamlWindowReference can still retrieve the window,
                    // just ignore it
                    if (window.AppWindow is not null)
                    {
                        window.SwitchTo();
                        window.AppWindow.MoveInZOrderAtTop();
                    }

                    return;
                }
        }
    }

    // Need more tests and feedbacks
    private static void CleanupLegacyMsixNotifyIconRegistryEntries()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(processPath))
            {
                return;
            }

            string executableName = Path.GetFileName(processPath);
            PackageId packageId = Package.Current.Id;
            string currentPackageFullName = packageId.FullName;
            string packageName = packageId.Name;

            string packageFamilyName = packageId.FamilyName;
            int separatorIndex = packageFamilyName.LastIndexOf('_');
            if (separatorIndex < 0)
            {
                return;
            }

            string publisherId = packageFamilyName[(separatorIndex + 1)..];

            using RegistryKey? notifyIconSettings = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", writable: true);
            if (notifyIconSettings is null)
            {
                return;
            }

            List<string> legacySubKeys = [];
            int? promoted = null;

            foreach (string subKeyName in notifyIconSettings.GetSubKeyNames())
            {
                using RegistryKey? subKey = notifyIconSettings.OpenSubKey(subKeyName);
                string? executablePath = subKey?.GetValue("ExecutablePath") as string;
                if (string.IsNullOrEmpty(executablePath))
                {
                    continue;
                }

                if (!IsSameMsixApp(executablePath, executableName, packageName, publisherId))
                {
                    continue;
                }

                if (subKey.GetValue("IsPromoted") is int isPromoted)
                {
                    promoted = Math.Max(promoted ?? 0, isPromoted);
                }

                if (!executablePath.Contains($"\\{currentPackageFullName}\\", StringComparison.OrdinalIgnoreCase))
                {
                    legacySubKeys.Add(subKeyName);
                }
            }

            foreach (string legacySubKey in legacySubKeys)
            {
                notifyIconSettings.DeleteSubKeyTree(legacySubKey, throwOnMissingSubKey: false);
            }

            if (promoted is not null)
            {
                foreach (string subKeyName in notifyIconSettings.GetSubKeyNames())
                {
                    using RegistryKey? subKey = notifyIconSettings.OpenSubKey(subKeyName, writable: true);
                    string? executablePath = subKey?.GetValue("ExecutablePath") as string;
                    if (string.IsNullOrEmpty(executablePath))
                    {
                        continue;
                    }

                    if (executablePath.Contains($"\\{currentPackageFullName}\\", StringComparison.OrdinalIgnoreCase)
                        && IsSameMsixApp(executablePath, executableName, packageName, publisherId))
                    {
                        subKey.SetValue("IsPromoted", promoted.Value, RegistryValueKind.DWord);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }

        static bool IsSameMsixApp(string executablePath, string executableName, string packageName, string publisherId)
        {
            return executablePath.EndsWith($"\\{executableName}", StringComparison.OrdinalIgnoreCase)
                && executablePath.Contains($"\\WindowsApps\\{packageName}_", StringComparison.OrdinalIgnoreCase)
                && executablePath.Contains($"__{publisherId}\\", StringComparison.OrdinalIgnoreCase);
        }
    }
}