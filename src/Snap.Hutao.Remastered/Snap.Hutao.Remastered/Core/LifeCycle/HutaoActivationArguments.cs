// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;

namespace Snap.Hutao.Remastered.Core.LifeCycle;

public sealed class HutaoActivationArguments
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
                        result.LaunchActivatedArguments = arguments;
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

    /// <summary>
    /// Reads the activation carried by the command line. The administrator restart forwards the
    /// activation as an argument, see <c>Bootstrap.TryRestartAsAdministrator</c>.
    /// </summary>
    internal static HutaoActivationArguments? TryCreateFromCommandLineArguments(IReadOnlyList<string> args)
    {
        if (args.Count is 0)
        {
            return null;
        }

        string? argument = args[0];
        if (string.IsNullOrEmpty(argument))
        {
            return null;
        }

        if (argument.StartsWith("hutao://", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(argument, UriKind.Absolute, out Uri? uri))
        {
            return new()
            {
                Kind = HutaoActivationKind.Protocol,
                ProtocolActivatedUri = uri,
            };
        }

        return new()
        {
            Kind = HutaoActivationKind.Launch,
            LaunchActivatedArguments = argument,
        };
    }

    /// <summary>
    /// Preserves native activation and, for Launch events, restores a <c>hutao://</c> URI carried
    /// by the parsed process arguments, including arguments forwarded by an administrator restart.
    /// </summary>
    internal static HutaoActivationArguments ResolveFromAppActivationArguments(AppActivationArguments args, IReadOnlyList<string> commandLineArguments, bool isRedirected = false)
    {
        HutaoActivationArguments result = FromAppActivationArguments(args, isRedirected);

        if (result.Kind is not HutaoActivationKind.Launch)
        {
            return result;
        }

        // Main's arguments (or Environment.GetCommandLineArgs()[1..]) have already been split
        // and unquoted by the runtime. Use those to restore the protocol activation without
        // assuming the app-model launch payload has the same shape as the process command line.
        HutaoActivationArguments? forwarded = TryCreateFromCommandLineArguments(commandLineArguments);
        if (forwarded is not { Kind: HutaoActivationKind.Protocol })
        {
            // Keep supporting an app-model launch payload that is itself a bare URI.
            forwarded = TryCreateFromCommandLineArguments([result.LaunchActivatedArguments ?? string.Empty]);
        }

        if (forwarded is not { Kind: HutaoActivationKind.Protocol })
        {
            return result;
        }

        forwarded.IsRedirectTo = isRedirected;
        return forwarded;
    }

    /// <summary>
    /// Serializes an already resolved activation for an administrator restart. Other activation
    /// kinds must stay in this process so their payload and user input are not discarded.
    /// </summary>
    internal bool TryGetAdministratorRestartArguments(out string arguments)
    {
        arguments = string.Empty;
        if (Kind is HutaoActivationKind.Protocol && ProtocolActivatedUri is { } uri)
        {
            arguments = $"\"{uri.OriginalString}\"";
            return true;
        }

        return Kind is HutaoActivationKind.Launch;
    }
}
