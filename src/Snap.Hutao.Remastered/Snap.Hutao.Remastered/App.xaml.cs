// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.Service.Job;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.Service.Update;
using Snap.Hutao.Remastered.UI.Xaml.Behavior;
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
    private bool exitRequested;
    private DateTime exitingTime;

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
        if (exitRequested) return;
        exitRequested = true;
        XamlApplicationLifetime.Exiting = true;
        FlyoutOpenStateBinding.CancelPendingSwitches();
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Application exiting", "Hutao"));
        SpinWait.SpinUntil(ShouldExitNow);
        ExitAfterContentDetachedAsync().SafeForget();
    }
    private async Task ExitAfterContentDetachedAsync()
    {
        await serviceProvider.GetRequiredService<MsixUpdateProgress>().StopAsync().ConfigureAwait(true);
        try { await serviceProvider.GetRequiredService<PluginService>().ShutdownAsync().ConfigureAwait(true); }
        catch (Exception error) { SentrySdk.CaptureException(error); }
        // Shutdown must happen before Application.Start returns. Quartz job setup
        // and running jobs may otherwise await a dispatcher already destroyed.
        await serviceProvider.GetRequiredService<IQuartzService>().ShutdownAsync().ConfigureAwait(true);
        await WindowExtension.DetachAllContentAsync().ConfigureAwait(true);
        // Leave the final Window.Closed callback before stopping its dispatcher.
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() => base.Exit());
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // DebugPatchXamlDiagnosticsRemoveRootObjectFromLVT();

        try
        {
            HutaoActivationArguments? activationArgs = null;

            if (RuntimeEnvironment.IsPackaged)
            {
                // Register before reading AppInstance activation arguments so the SDK
                // correctly identifies packaged app-notification activations.
                AppNotificationManager.Default.NotificationInvoked += activation.NotificationInvoked;
                AppNotificationManager.Default.Register();
                activationArgs = HutaoActivationArguments.FromAppActivationArguments(AppInstance.GetCurrent().GetActivatedEventArgs());
            }
            else
            {
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
                        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.ico");
                        Uri iconUri = new(iconPath);

                        AppNotificationManager.Default.Register(displayName, iconUri);
                        AppNotificationManager.Default.NotificationInvoked += activation.NotificationInvoked;
                    }
                    catch
                    {
                        // Notification registration is best-effort in unpackaged mode.
                    }
                }

                // The raw command line is authoritative for unpackaged hutao://
                // activations, which the SDK can otherwise report as Launch.
                activationArgs = TryDetectUnpackagedActivation();
                if (activationArgs is null)
                {
                    try
                    {
                        activationArgs = HutaoActivationArguments.FromAppActivationArguments(AppInstance.GetCurrent().GetActivatedEventArgs());
                    }
                    catch
                    {
                        // Some unpackaged environments cannot provide SDK activation args.
                    }
                }
            }

            activationArgs ??= new() { Kind = HutaoActivationKind.Launch, LaunchActivatedArguments = string.Empty };
            activationArgs.IsRedirectTo = false;

            logger.LogInformation($"{ConsoleBanner}");
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Activate and Initialize", "Application"));

            // Schedule first activation before draining any redirections which arrived
            // while DI and the XAML dispatcher were starting.
            activation.ActivateAndInitialize(activationArgs);
            Bootstrap.SetActivationHandler(activation);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            SentrySdk.Flush();
            ProcessFactory.KillCurrent();
        }
    }

    private bool ShouldExitNow()
    {
        if (exitingTime == default)
        {
            exitingTime = DateTime.UtcNow;
        }

        return XamlApplicationLifetime.ActivationAndInitializationCompleted || DateTime.UtcNow >= exitingTime.AddSeconds(5);
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
    private void DebugPatchXamlDiagnosticsRemoveRootObjectFromLVT()
    {
        // Extremely dangerous patch to workaround XamlDiagnostics::RemoveRootObjectFromLVT crashing when
        // Window is closed during debugging. at LiveVisualTree.cpp line 423
        // -> if (m_visualTreeCallback && SUCCEEDED(m_visualTreeCallback.As(&xamlRootCallback)))
        // We simply fail this check to skip the rest if block.
        // As a result, Visual Studio Live Visual Tree can leave a DesktopWindowXamlSource without child.
        // But the RuntimeObject is actually closed properly.

        // If no debugger is attached, do not patch. There will be no diagnostics LVT.
        if (!Debugger.IsAttached)
        {
            return;
        }

        // The RVA changes on every WindowsAppSDK update, re-derive it with the byte sequence below
        // (unique in the whole module) and then adjust the offset.
        // Microsoft.ui.xaml.dll 3.2.0.2511
        // XamlDiagnostics::RemoveRootObjectFromLVT + 0x132
        // 74 65            jz      short loc_18090C48D
        // 48 8D 55 F0      lea     rdx, [rbp-10h] ; p
        // 48 8B CB         mov     rcx, rbx ; this
        // E8 00 DF FF FF   call    ??$As @UIVisualTreeServiceCallback3@@@?$ComPtr @UIVisualTreeServiceCallback@@@WRL @Microsoft@@QEBAJV ?$ComPtrRef @V?$ComPtr @UIVisualTreeServiceCallback3@@@WRL @Microsoft@@@Details@12@@Z; Microsoft::WRL::ComPtr < IVisualTreeServiceCallback >::As<IVisualTreeServiceCallback3>(Microsoft::WRL::Details::ComPtrRef<Microsoft::WRL::ComPtr<IVisualTreeServiceCallback3>>)
        // 85 C0            test    eax, eax
        // 78 55            js      short loc_18090C48D
        // Should be 78 xx (js near)
        Win32.MemoryUtilities.Patch("Microsoft.ui.xaml.dll", 0x0090C436, 3, codes =>
        {
            // Rewrite js to jmp
            codes[2] = 0xEB;
        });
    }
}
