// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;
using System.Runtime.InteropServices;

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

    private static string GetFirstUnpackagedArgument(string commandLine)
    {
        // The SDK supplies the *full* command line, including the executable.
        // CommandLineToArgvW implements the shell's Windows quoting rules.
        nint argv = CommandLineToArgvW(commandLine, out int count);
        if (argv == 0)
        {
            return string.Empty;
        }

        try
        {
            return count > 1 ? Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, IntPtr.Size)) ?? string.Empty : string.Empty;
        }
        finally
        {
            _ = LocalFree(argv);
        }
    }

    [LibraryImport("shell32.dll", EntryPoint = "CommandLineToArgvW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CommandLineToArgvW(string commandLine, out int argumentCount);

    [LibraryImport("kernel32.dll")]
    private static partial nint LocalFree(nint handle);
}
