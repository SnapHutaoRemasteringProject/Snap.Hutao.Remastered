// Only the WinRT activation payload adapter is substituted. Parsing, activation
// precedence and redirect flags are exercised through the linked production class.

namespace Microsoft.Windows.AppLifecycle;

public enum ExtendedActivationKind
{
    Launch,
    Protocol,
    AppNotification,
    File,
}

public sealed class AppActivationArguments
{
    public ExtendedActivationKind Kind { get; init; }

    public string Arguments { get; init; } = string.Empty;

    public Uri? Uri { get; init; }

    public IDictionary<string, string> NotificationArguments { get; init; } = new Dictionary<string, string>();

    public IDictionary<string, string> UserInput { get; init; } = new Dictionary<string, string>();

    public bool TryGetLaunchActivatedArguments([NotNullWhen(true)] out string? arguments)
    {
        arguments = Arguments.Trim();
        return Kind is ExtendedActivationKind.Launch;
    }

    public bool TryGetProtocolActivatedUri([NotNullWhen(true)] out Uri? uri)
    {
        uri = Uri;
        return Kind is ExtendedActivationKind.Protocol && uri is not null;
    }

    public bool TryGetAppNotificationActivatedArguments(out string? argument, [NotNullWhen(true)] out IDictionary<string, string>? arguments, [NotNullWhen(true)] out IDictionary<string, string>? userInput)
    {
        argument = Arguments;
        arguments = NotificationArguments;
        userInput = UserInput;
        return Kind is ExtendedActivationKind.AppNotification;
    }
}
