// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
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
using System.Runtime.InteropServices;
using System.Security.Principal;
using WinRT;

[assembly: DisableRuntimeMarshalling]

namespace Snap.Hutao.Remastered;

[SuppressMessage("", "SH001")]
public static partial class Bootstrap
{
    private static readonly ApplicationInitializationCallback AppInitializationCallback = InitializeApp;
    private static readonly object ActivationSyncRoot = new();
    private static readonly Queue<HutaoActivationArguments> PendingActivations = new();
    private static IAppActivation? activationHandler;

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

        // Sentry initializes before the redirect so the second instance can report how long
        // handing the activation to the running instance takes. ComWrappers are initialized
        // first because Sentry reads the WebView2 version while configuring its scope.
        ComWrappersSupport.InitializeComWrappers();
        using IDisposable telemetry = LoggerFactoryExtension.InitializeSentry();

        // WinAppSDK atomically assigns the single-instance key. Unlike the old
        // open-then-create event check, concurrent launches cannot both become the owner.
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey("Snap.Hutao.Remastered" + AppDataPaths.InstanceSuffix);
        if (!keyInstance.IsCurrent)
        {
            using (SentrySpanScope redirect = SentrySpanScope.StartTransaction("app.startup.redirect", "app.startup"))
            {
                redirect.SetData("startup.has_running_instance", true);
                try
                {
                    // For packaged toast activations, register before reading activation
                    // arguments so the SDK can identify notification activation correctly.
                    if (RuntimeEnvironment.IsPackaged)
                    {
                        try
                        {
                            AppNotificationManager.Default.Register();
                        }
                        catch (Exception ex)
                        {
                            // Failed notification registration must not prevent
                            // redirection of ordinary launches or URI activations.
                            SentrySdk.CaptureException(ex);
                        }
                    }

                    AppActivationArguments activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                    RedirectActivationTo(keyInstance, activatedArgs);
                    redirect.SetData("startup.redirected", true);
                }
                catch (Exception ex)
                {
                    redirect.SetData("startup.redirected", false);
                    SentrySdk.CaptureException(ex);
                }
            }

            // The secondary process must not run XAML startup or become another instance.
            SentrySdk.Flush();
            return;
        }

        // Register before initializing the XAML application. Redirections arriving while
        // the app/DI is being constructed are buffered until OnLaunched is ready.
        AppInstance.GetCurrent().Activated += OnAppInstanceActivated;
        try
        {
            if (ShouldRestartAsAdmin())
            {
                // Release ownership before starting the elevated child.
                AppInstance.GetCurrent().UnregisterKey();
                RestartAsAdministrator(args);
                return;
            }

            ShowLowWindowsVersionWarningOnce();

            Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "00000000");
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXARRAYSPERPARTITION", "128");
            AppContext.SetData("MVVMTOOLKIT_ENABLE_INOTIFYPROPERTYCHANGING_SUPPORT", false);

            // By adding the using statement, we can dispose the injected services when closing
            using (ServiceProvider serviceProvider = DependencyInjection.Initialize(true))
            {
                Thread.CurrentThread.Name = "Snap Hutao Remastered Application Main Thread";

                // Register URL protocol for unpackaged mode
                RuntimeEnvironment.TryRegisterProtocol();

                // Microsoft.UI.Xaml.Settings.XamlOptionalChanges.EnableChange(Microsoft.UI.Xaml.Settings.XamlChangeId.DefaultStyleOptimizations);
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
        finally
        {
            AppInstance.GetCurrent().Activated -= OnAppInstanceActivated;
            AppInstance.GetCurrent().UnregisterKey();
            lock (ActivationSyncRoot)
            {
                activationHandler = null;
                PendingActivations.Clear();
            }
        }
    }

    /// <summary>Connect the WASDK activation event to the initialized application.</summary>
    internal static void SetActivationHandler(IAppActivation handler)
    {
        lock (ActivationSyncRoot)
        {
            activationHandler = handler;
            while (PendingActivations.TryDequeue(out HutaoActivationArguments? pending))
            {
                handler.RedirectedActivate(pending);
            }
        }
    }

    private static void OnAppInstanceActivated(object? sender, AppActivationArguments args)
    {
        try
        {
            HutaoActivationArguments redirected = HutaoActivationArguments.FromAppActivationArguments(args, isRedirected: true);
            lock (ActivationSyncRoot)
            {
                if (activationHandler is { } handler)
                {
                    handler.RedirectedActivate(redirected);
                }
                else
                {
                    PendingActivations.Enqueue(redirected);
                }
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void RedirectActivationTo(AppInstance target, AppActivationArguments args)
    {
        // Main is STA. Block with a COM-aware wait rather than synchronously waiting
        // on a WinRT async operation on the STA, which can deadlock.
        using ManualResetEvent completed = new(false);
        Task redirectTask = Task.Run(async () =>
        {
            try
            {
                await target.RedirectActivationToAsync(args);
            }
            finally
            {
                completed.Set();
            }
        });

        nint eventHandle = completed.SafeWaitHandle.DangerousGetHandle();
        int hr = CoWaitForMultipleHandles(0, uint.MaxValue, 1, in eventHandle, out _);
        Marshal.ThrowExceptionForHR(hr);
        redirectTask.GetAwaiter().GetResult();
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoWaitForMultipleHandles(uint flags, uint timeout, uint handleCount, in nint handles, out uint index);

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
            SentrySdk.Flush();
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
