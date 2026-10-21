// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO.Http.Proxy;
using Snap.Hutao.Remastered.Win32;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Core.Logging;

public static class LoggerFactoryExtension
{
    public static IDisposable InitializeSentry()
    {
        IDisposable sdk = SentrySdk.Init(options =>
        {
            options.HttpProxy = HttpProxyUsingSystemProxy.Instance;

#if DEBUG || IS_ALPHA_BUILD || IS_CANARY_BUILD
            // Alpha and Canary produces noisy events
            options.Dsn = "https://87c60195ae5a60f0038387547f7cd034@o4510769861820416.ingest.us.sentry.io/4510844382871552";
#else
            options.Dsn = "https://87c60195ae5a60f0038387547f7cd034@o4510769861820416.ingest.us.sentry.io/4510844382871552";
#endif

#if DEBUG
            options.Debug = true;
#endif

            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.EnableBackpressureHandling = true;
            options.Release = $"{HutaoRuntime.Version}";
            options.Environment = GetBuildEnvironment();

            // Structured logs are enabled by default in v7. Preserve the existing policy.
            options.SetBeforeSendLog(static _ => null);

            options.ProfilesSampleRate = 1.0D;
            options.TracesSampleRate = 1.0D;

            // Use our own exception handling
            options.DisableWinUiUnhandledExceptionIntegration();

            options.AddExceptionProcessor(new SentryExceptionProcessor());

            options.SetBeforeSend(@event =>
            {
                Sentry.Protocol.OperatingSystem operatingSystem = @event.Contexts.OperatingSystem;
                HutaoPrivateWindowsVersion windowsVersion = HutaoNative.Instance.GetCurrentWindowsVersion();
                operatingSystem.Build = $"{windowsVersion.Build}";
                operatingSystem.Name = "Windows";
                operatingSystem.Version = $"{windowsVersion}";

                return @event;
            });
        });

        SentrySdk.ConfigureScope(static scope =>
        {
            scope.User = new() { Id = HutaoRuntime.DeviceId };
            scope.SetTag("elevated", HutaoRuntime.IsProcessElevated ? "yes" : "no");
            scope.SetWebView2Version();
        });
        return sdk;
    }

    extension(ILoggingBuilder builder)
    {
        public ILoggingBuilder AddSentryTelemetry()
        {
            return builder.AddSentry(static options =>
            {
                options.MinimumBreadcrumbLevel = LogLevel.None;
                options.MinimumEventLevel = LogLevel.None;
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetBuildEnvironment()
    {
#if DEBUG
        return "DEBUG";
#elif IS_ALPHA_BUILD
        return "ALPHA";
#elif IS_CANARY_BUILD
        return "CANARY";
#else
        return "RELEASE";
#endif
    }

    extension(Scope scope)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetWebView2Version()
        {
            WebView2Version webView2Version = HutaoRuntime.WebView2Version;
            Dictionary<string, object> webView2 = new()
            {
                ["Supported"] = webView2Version.Supported,
                ["Version"] = webView2Version.RawVersion,
            };

            scope.Contexts["WebView2"] = webView2;
        }
    }
}
