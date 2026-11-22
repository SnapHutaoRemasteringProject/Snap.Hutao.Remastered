// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;
using Win32LocalHandle = global::Windows.Win32.Foundation.HLOCAL;
using Win32PInvoke = global::Windows.Win32.PInvoke;
using Win32String = global::Windows.Win32.Foundation.PWSTR;

namespace Snap.Hutao.Remastered.Core.LifeCycle;

public sealed partial class HutaoActivationArguments
{
    public bool IsRedirectTo { get; set; }

    public HutaoActivationKind Kind { get; set; }

    public Uri? ProtocolActivatedUri { get; set; }

    public string? LaunchActivatedArguments { get; set; }

    public IReadOnlyDictionary<string, string>? AppNotificationActivatedArguments { get; set; }

    public IReadOnlyDictionary<string, string>? AppNotificationActivatedUserInput { get; set; }

    public static HutaoActivationArguments FromAppActivationArguments(AppActivationArguments args, bool isRedirected = false)
    {
        HutaoActivationArguments result = new()
        {
            IsRedirectTo = isRedirected,
        };

        switch (args.Kind)
        {
            case ExtendedActivationKind.Launch:
                {
                    result.Kind = HutaoActivationKind.Launch;
                    if (args.TryGetLaunchActivatedArguments(out string? arguments))
                    {
                        // Unpackaged protocol launches use the raw Win32 command line
                        // and may be classified as Launch by Windows App SDK.
                        string launchArgument = RuntimeEnvironment.IsUnpackaged
                            ? GetFirstUnpackagedArgument(arguments)
                            : arguments;

                        if (RuntimeEnvironment.IsUnpackaged && launchArgument.StartsWith("hutao://", StringComparison.OrdinalIgnoreCase)
                            && Uri.TryCreate(launchArgument, UriKind.Absolute, out Uri? uri))
                        {
                            result.Kind = HutaoActivationKind.Protocol;
                            result.ProtocolActivatedUri = uri;
                        }
                        else
                        {
                            result.LaunchActivatedArguments = launchArgument;
                        }
                    }

                    break;
                }

            case ExtendedActivationKind.Protocol:
                {
                    result.Kind = HutaoActivationKind.Protocol;
                    if (args.TryGetProtocolActivatedUri(out Uri? uri))
                    {
                        result.ProtocolActivatedUri = uri;
                    }

                    break;
                }

            case ExtendedActivationKind.AppNotification:
                {
                    result.Kind = HutaoActivationKind.AppNotification;
                    if (args.TryGetAppNotificationActivatedArguments(out string? argument, out IDictionary<string, string>? arguments, out IDictionary<string, string>? userInput))
                    {
                        result.LaunchActivatedArguments = argument;
                        result.AppNotificationActivatedArguments = arguments.AsReadOnly();
                        result.AppNotificationActivatedUserInput = userInput.AsReadOnly();
                    }

                    break;
                }
        }

        return result;
    }

    private static unsafe string GetFirstUnpackagedArgument(string commandLine)
    {
        // The SDK supplies the *full* command line, including the executable.
        // CommandLineToArgvW implements the shell's Windows quoting rules.
        Win32String* argv = Win32PInvoke.CommandLineToArgv(commandLine, out int count);
        if (argv is null)
        {
            return string.Empty;
        }

        try
        {
            return count > 1 ? new string(argv[1].Value) : string.Empty;
        }
        finally
        {
            _ = Win32PInvoke.LocalFree(new Win32LocalHandle(argv));
        }
    }

}
