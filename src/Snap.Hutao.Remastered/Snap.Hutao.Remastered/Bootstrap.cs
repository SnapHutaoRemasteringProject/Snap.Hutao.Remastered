// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Security.Principal;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service.Job;
using Snap.Hutao.Remastered.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using WinRT;

[assembly: DisableRuntimeMarshalling]

namespace Snap.Hutao.Remastered;

[SuppressMessage("", "SH001")]
public static partial class Bootstrap
{
    private const string LockName = "SNAP_HUTAO_BOOTSTRAP_LOCK";
    private static readonly ApplicationInitializationCallback AppInitializationCallback = InitializeApp;
    private static EventWaitHandle? instanceHandle;

    public static void UseNamedPipeRedirection()
    {
        // Clear the field without disposing so the kernel object stays alive
        // for other processes to detect. The using() in Main() handles cleanup.
        instanceHandle = null;
    }

    [STAThread]
    private static void Main(string[] args)
    {
        // Check for toast notification helper process (non-elevated, launched via explorer.exe).
        // The main elevated process creates a named pipe and writes the toast XML before
        // launching this helper. If we can connect to the pipe, we're in helper mode.
        if (TryHandleToastNotificationHelper())
        {
            return;
        }

        bool hasRunningInstance = HasRunningInstance();

        // 1. Another instance is already running, hand this activation over to it.
        // If the redirect fails, fall through to full startup: the pipe server may not be ready yet
        // during first instance init.
        if (hasRunningInstance && PrivateNamedPipeClient.TryLightweightRedirect(args))
        {
            return;
        }

        // 2. Check if we should restart as administrator. The command-line arguments carry the
        // activation (hutao:// URI, notification arguments, ...) and are passed on to the restarted
        // instance, so it continues what this process was started for.
        if (ShouldRestartAsAdmin())
        {
            RestartAsAdministrator(args);
            return;
        }

        // 3. Without an instance to hand over to, this process becomes the single instance. Its own
        // activation is read from the command-line arguments by the application.
        if (!hasRunningInstance)
        {
            try
            {
                EventWaitHandleSecurity handleSecurity = new();
                handleSecurity.AddAccessRule(new(SecurityIdentifiers.Everyone, EventWaitHandleRights.FullControl, AccessControlType.Allow));

                // The security descriptor is applied while the lock is created: setting it afterwards
                // leaves a window in which another instance sees the default descriptor of this
                // process, which an unelevated instance of the same user cannot open.
                instanceHandle = EventWaitHandleAcl.Create(false, EventResetMode.ManualReset, LockName, out bool created, handleSecurity);
                Debug.Assert(created);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                // Another instance created the lock in the meantime and this process may not open it.
                SentrySdk.CaptureException(ex);
                return;
            }
        }

        using (instanceHandle)
        {
            ShowLowWindowsVersionWarningOnce();

            Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "00000000");
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXARRAYSPERPARTITION", "128");
            AppContext.SetData("MVVMTOOLKIT_ENABLE_INOTIFYPROPERTYCHANGING_SUPPORT", false);

            ComWrappersSupport.InitializeComWrappers();

            using IDisposable telemetry = LoggerFactoryExtension.InitializeSentry();

            // By adding the using statement, we can dispose the injected services when closing
            using (ServiceProvider serviceProvider = DependencyInjection.Initialize(true))
            {
                Thread.CurrentThread.Name = "Snap Hutao Remastered Application Main Thread";

                // Register URL protocol for unpackaged mode
                RuntimeEnvironment.TryRegisterProtocol();

                Microsoft.UI.Xaml.Settings.XamlOptionalChanges.EnableChange(Microsoft.UI.Xaml.Settings.XamlChangeId.DefaultStyleOptimizations);
                Microsoft.UI.Xaml.Settings.XamlOptionalChanges.EnableChange(Microsoft.UI.Xaml.Settings.XamlChangeId.DeferContextFlyoutInit);
                Microsoft.UI.Xaml.Settings.XamlOptionalChanges.EnableChange(Microsoft.UI.Xaml.Settings.XamlChangeId.IconNoGridOptimization);
                // Microsoft.UI.Xaml.Settings.XamlOptionalChanges.EnableChange(Microsoft.UI.Xaml.Settings.XamlChangeId.OptimizeApplyStyles);

                // If you hit a COMException REGDB_E_CLASSNOTREG (0x80040154) during debugging
                // You can delete bin and obj folder and then rebuild.
                // In a Desktop app this runs a message pump publicly,
                // and does not return until the application shuts down.
                try
                {
                    Application.Start(AppInitializationCallback);
                }
                finally
                {
                    // Also cover natural dispatcher shutdown and restart paths
                    // which do not pass through App.Exit. Cancel before disposing
                    // the root container, then join Quartz's bounded shutdown.
                    XamlApplicationLifetime.Exiting = true;
                    serviceProvider.GetRequiredService<IQuartzService>().ShutdownAsync().GetAwaiter().GetResult();
                    XamlApplicationLifetime.Exited = true;
                }
            }

            SentrySdk.Flush();
        }
    }

    private static void InitializeApp(ApplicationInitializationCallbackParams param)
    {
        Gen2GcCallback.Register(() =>
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug("Gen2 GC triggered.", "Runtime"));
            return true;
        });

        IServiceProvider serviceProvider = Ioc.Default;

        _ = serviceProvider.GetRequiredService<ITaskContext>();
        _ = serviceProvider.GetRequiredService<App>();
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Whether an instance of the application is already running. The lock is only ever used as an
    /// existence marker, so failing to open it means that someone else owns it as well.
    /// </summary>
    private static bool HasRunningInstance()
    {
        try
        {
            return EventWaitHandle.TryOpenExisting(LockName, out _);
        }
        catch (UnauthorizedAccessException ex)
        {
            SentrySdk.CaptureException(ex);
            return true;
        }
    }

    private static bool ShouldRestartAsAdmin()
    {
        try
        {
            if (IsRunningAsAdministrator())
            {
                return false;
            }

            return LocalSetting.Get(SettingKeys.AutoRestartAsAdmin, false);
        }
        catch
        {
            return false;
        }
    }

    private static void RestartAsAdministrator(string[] args)
    {
        try
        {
            string? currentProcessPath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            ArgumentNullException.ThrowIfNull(currentProcessPath);

            string forwardedArguments = RuntimeEnvironment.IsUnpackaged
                ? GetForwardedArguments(args)
                : GetAppModelForwardedArguments();

            ProcessFactory.StartUsingShellExecuteRunAs(currentProcessPath, forwardedArguments);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static string GetForwardedArguments(string[] args)
    {
        return string.Join(' ', args.Select(static arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));
    }

    private static string GetAppModelForwardedArguments()
    {
        try
        {
            ComWrappersSupport.InitializeComWrappers();

            HutaoActivationArguments activation = HutaoActivationArguments.FromAppActivationArguments(AppInstance.GetCurrent().GetActivatedEventArgs());
            return activation switch
            {
                { Kind: HutaoActivationKind.Protocol, ProtocolActivatedUri: { } uri } => $"\"{uri.OriginalString}\"",
                _ => string.Empty,
            };
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return string.Empty;
        }
    }

    private static bool TryHandleToastNotificationHelper()
    {
        using ToastNotificationPipeClient pipeClient = new();
        ToastNotificationRequest? request = pipeClient.TryGetRequest();

        if (request is null)
        {
            return false;
        }

        bool registered = false;

        try
        {
            ComWrappersSupport.InitializeComWrappers();

            string displayName =
#if DEBUG
                "Snap Hutao Remastered Dev";
#else
                "Snap Hutao Remastered";
#endif
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.ico");
            Uri iconUri = new(iconPath);

            AppNotificationManager.Default.Register(displayName, iconUri);
            registered = true;
        }
        catch
        {
            // Toast helper failure is non-fatal
        }

        try
        {
            AppNotification notification = new(request.RawXml)
            {
                SuppressDisplay = request.SuppressDisplay,
            };
            AppNotificationManager.Default.Show(notification);
            Thread.Sleep(500);
        }
        catch
        {
            // Toast helper failure is non-fatal
        }
        finally
        {
            if (registered)
            {
                try
                {
                    AppNotificationManager.Default.Unregister();
                }
                catch
                {
                    // Toast helper failure is non-fatal
                }
            }
        }

        return true;
    }

    private static void ShowLowWindowsVersionWarningOnce()
    {
        if (HutaoNative.Instance.IsCurrentWindowsVersionSupported())
        {
            return;
        }

        if (!LocalSetting.Get(SettingKeys.LowWindowsVersionWarningShown, false))
        {
            const string Message = """
                Snap Hutao Remastered 无法在版本低于 10.0.19045.5371 的 Windows 上运行，请更新系统。
                Snap Hutao Remastered cannot run on Windows versions earlier than 10.0.19045.5371. Please update your system.
                """;
            HutaoNative.Instance.ShowErrorMessage("Warning | 警告", Message);
            LocalSetting.Set(SettingKeys.LowWindowsVersionWarningShown, true);
        }
    }
}
