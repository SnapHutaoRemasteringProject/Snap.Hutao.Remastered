// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.UI.Xaml;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using System.Diagnostics;
using System.IO;

namespace Snap.Hutao.Remastered;

[Service(ServiceLifetime.Singleton)]
[SuppressMessage("", "SH001", Justification = "The App must be public")]
public sealed partial class App : Application
{
    private const string ConsoleBanner = """
        ----------------------------------------------------------------
          _____                         _    _         _ 
         / ____|                       | |  | |       | |
        | (___   _ __    __ _  _ __    | |__| | _   _ | |_  __ _   ___
         \___ \ | '_ \  / _` || '_ \   |  __  || | | || __|/ _` | / _ \
         ____) || | | || (_| || |_) |_ | |  | || |_| || |_| (_| || (_) |
        |_____/ |_| |_| \__,_|| .__/(_)|_|  |_| \__,_| \__|\__,_| \___/
                              | |
                              |_|
        
        Snap.Hutao.Remastered is a open source software developed by DGP Studio.
        Copyright (C) 2022 - 2025 DGP Studio, All Rights Reserved.
        Copyright (C) 2026 SnapHutaoRemasteringProject, All Rights Reserved.
        ----------------------------------------------------------------
        """;

    private readonly IServiceProvider serviceProvider;
    private readonly IAppActivation activation;
    private readonly ILogger<App> logger;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial App(IServiceProvider serviceProvider);

    public App()
    {
        // Fuck Microsoft.
        // Publish without testing.
        throw new NotImplementedException("This constructor is only for XAML compilation.");
    }

    /// <summary>
    /// Shortcut to get the <see cref="AppOptions"/> instance.
    /// </summary>
    public partial AppOptions Options { get; }

    partial void PostConstruct(IServiceProvider serviceProvider)
    {
        ExceptionHandling.Initialize(serviceProvider, this);
    }

    [SuppressMessage("", "SA1202")]
    public new void Exit()
    {
        XamlApplicationLifetime.Exiting = true;
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Application exiting", "Hutao"));
        // #114
        //SpinWait.SpinUntil(static () => XamlApplicationLifetime.ActivationAndInitializationCompleted);
        base.Exit();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // DebugPatchXamlDiagnosticsRemoveRootObjectFromLVT();

        try
        {
            AppActivationArguments? activatedEventArgs;

            if (RuntimeEnvironment.IsPackaged)
            {
                // Important: You must call AppNotificationManager::Default().Register
                // before calling AppInstance.GetCurrent.GetActivatedEventArgs.
                AppNotificationManager.Default.NotificationInvoked += activation.NotificationInvoked;
                AppNotificationManager.Default.Register();

                // E_INVALIDARG E_OUTOFMEMORY
                activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

                if (serviceProvider.GetRequiredService<PrivateNamedPipeClient>().TryRedirectActivationTo(activatedEventArgs))
                {
                    SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Application exiting on RedirectActivationTo", "Hutao"));
                    XamlApplicationLifetime.ActivationAndInitializationCompleted = true;
                    Exit();

                    // base.Exit() may not fully terminate the process in unpackaged mode,
                    // so force exit to guarantee the new instance terminates completely.
                    SentrySdk.Flush();
                    Environment.Exit(0);
                    return;
                }
            }
            else
            {
                // In unpackaged mode, register toast notifications.
                // This must happen before GetActivatedEventArgs() so WASDK can
                // detect notification-based activations.
                if (TryRegisterUnpackagedNotification())
                {
                    try
                    {
                        string displayName =
#if DEBUG
                            "Snap Hutao Remastered Dev";
#else
                            "Snap Hutao Remastered";
#endif
                        string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.ico");
                        Uri iconUri = new(iconPath);

                        AppNotificationManager.Default.Register(displayName, iconUri);
                        AppNotificationManager.Default.NotificationInvoked += activation.NotificationInvoked;
                    }
                    catch
                    {
                        // Notification registration is best-effort in unpackaged mode
                    }
                }

                // In unpackaged mode, AppNotification APIs are not available.
                // AppInstance.GetCurrent() may also throw in some environments,
                // and cannot reliably distinguish Launch from Protocol activation.
                // We always check command-line args ourselves.
                HutaoActivationArguments? unpackagedArgs = TryDetectUnpackagedActivation();

                if (unpackagedArgs is not null)
                {
                    // Try to redirect activation to the already running instance
                    if (serviceProvider.GetRequiredService<PrivateNamedPipeClient>().TryRedirectActivationTo(unpackagedArgs))
                    {
                        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Application exiting on RedirectActivationTo", "Hutao"));
                        XamlApplicationLifetime.ActivationAndInitializationCompleted = true;
                        Exit();

                        // base.Exit() may not fully terminate the process in unpackaged mode,
                        // so force exit to guarantee the new instance terminates completely.
                        SentrySdk.Flush();
                        Environment.Exit(0);
                        return;
                    }

                    // Redirect failed (no running instance), activate directly
                    unpackagedArgs.IsRedirectTo = false;
                    activation.ActivateAndInitialize(unpackagedArgs);
                    return;
                }

                try
                {
                    activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                }
                catch
                {
                    activatedEventArgs = null;
                }
            }

            if (activatedEventArgs is null)
            {
                // Fallback for unpackaged mode: treat as a simple launch
                activation.ActivateAndInitialize(new() { Kind = HutaoActivationKind.Launch, LaunchActivatedArguments = string.Empty });
                return;
            }

            logger.LogInformation($"{ConsoleBanner}");

            FrameworkTheming.SetTheme(ThemeHelper.ElementToFramework(serviceProvider.GetRequiredService<AppOptions>().ElementTheme.Value));

            // Manually invoke
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Activate and Initialize", "Application"));
            activation.ActivateAndInitialize(HutaoActivationArguments.FromAppActivationArguments(activatedEventArgs));
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            SentrySdk.Flush();

            ProcessFactory.KillCurrent();
        }
    }

    /// <summary>
    /// In unpackaged mode, WASDK cannot reliably distinguish Launch from Protocol
    /// activation. Check command-line args ourselves to detect the real kind.
    /// </summary>
    private static HutaoActivationArguments? TryDetectUnpackagedActivation()
    {
        string[] cmdArgs = Environment.GetCommandLineArgs();
        string? arg = cmdArgs.Length > 1 ? cmdArgs[1] : null;

        if (arg is null)
        {
            return null;
        }

        if (arg.StartsWith("hutao://", StringComparison.OrdinalIgnoreCase))
        {
            return new()
            {
                Kind = HutaoActivationKind.Protocol,
                ProtocolActivatedUri = new Uri(arg),
                IsRedirectTo = true,
            };
        }

        return new()
        {
            Kind = HutaoActivationKind.Launch,
            LaunchActivatedArguments = arg,
            IsRedirectTo = true,
        };
    }

    /// <summary>
    /// Create or update a Start Menu shortcut with the app's AUMID so that
    /// WASDK can register the COM toast activator in unpackaged mode.
    /// </summary>
    private static bool TryRegisterUnpackagedNotification()
    {
        // WASDK scans the user's Start Menu Programs for shortcuts to
        // associate the process with a toast activator AUMID.
        string shortcutPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            "Snap.Hutao.Remastered.lnk");

        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        try
        {
            if (!File.Exists(shortcutPath))
            {
                string? directory = System.IO.Path.GetDirectoryName(shortcutPath);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }
            }

            // Always overwrite to ensure AUMID and target are up to date
            FileSystem.CreateLinkWithAppUserModelId(exePath, string.Empty, string.Empty, shortcutPath, HutaoRuntime.FamilyName);
            LocalSetting.Set(SettingKeys.IsUnpackagedNotificationRegistered, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Conditional("DEBUG")]
    private static void DebugPatchXamlDiagnosticsRemoveRootObjectFromLVT()
    {
        // Extremely dangerous patch to workaround XamlDiagnostics::RemoveRootObjectFromLVT crashing when
        // Window is closed during debugging. at LiveVisualTree.cpp line 423
        // -> if (m_visualTreeCallback && SUCCEEDED(m_visualTreeCallback.As(&xamlRootCallback)))
        // We simply fail this check to skip the rest if block.
        // As a result, Visual Studio Live Visual Tree can leave a DesktopWindowXamlSource without child.
        // But the RuntimeObject is actually closed properly.

        // If no debugger is attached, do not patch. There will be no diagnostics LVT.
        if (Debugger.IsAttached)
        {
            // 74 65            jz      short loc_8E219D
            // 48 8D 55 F0      lea     root, [rbp+50h + p] ; p
            // 48 8B CB         mov     this, rbx; this
            // E8 58 DF FF FF   call    ??$As @UIVisualTreeServiceCallback3@@@?$ComPtr @UIVisualTreeServiceCallback@@@WRL @Microsoft@@QEBAJV ?$ComPtrRef @V?$ComPtr @UIVisualTreeServiceCallback3@@@WRL @Microsoft@@@Details@12@@Z; Microsoft::WRL::ComPtr < IVisualTreeServiceCallback >::As<IVisualTreeServiceCallback3>(Microsoft::WRL::Details::ComPtrRef<Microsoft::WRL::ComPtr<IVisualTreeServiceCallback3>>)
            // 85 C0            test    eax, eax
            // 78 55            js      short loc_8E219D
            // Should be 78 xx (js near)
            Win32.MemoryUtilities.Patch("Microsoft.ui.xaml.dll", 0x0090C1D6, 2, static codes =>
            {
                // Rewrite to jmp
                codes[0] = 0xEB;
            });
        }
    }
}
