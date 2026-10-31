using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Core.LifeCycle;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AppActivationArgumentsTest
{
    private const string ForwardedUri = "hutao://launch/?uid=123456789&value=a%2Fb%20c";

    [TestMethod]
    [DataRow(ForwardedUri)]
    [DataRow("")]
    public void LaunchRestoresProtocolFromParsedProcessArguments(string rawLaunchArguments)
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Launch, Arguments = rawLaunchArguments };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri]);

        Assert.AreEqual(HutaoActivationKind.Protocol, result.Kind);
        Assert.AreEqual(ForwardedUri, result.ProtocolActivatedUri?.OriginalString);
        Assert.IsFalse(result.IsRedirectTo);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RestoredProtocolKeepsTheRedirectFlag(bool redirected)
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Launch };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri], redirected);

        Assert.AreEqual(HutaoActivationKind.Protocol, result.Kind);
        Assert.AreEqual(redirected, result.IsRedirectTo);
    }

    [TestMethod]
    public void BareAppModelUriIsPreservedWhenProcessArgumentsAreEmpty()
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Launch, Arguments = ForwardedUri };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, []);

        Assert.AreEqual(HutaoActivationKind.Protocol, result.Kind);
        Assert.AreEqual(ForwardedUri, result.ProtocolActivatedUri?.OriginalString);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NativeProtocolTakesPriorityOverProcessArguments(bool redirected)
    {
        Uri original = new("hutao://achievement/import");
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Protocol, Uri = original };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri], redirected);

        Assert.AreEqual(HutaoActivationKind.Protocol, result.Kind);
        Assert.AreSame(original, result.ProtocolActivatedUri);
        Assert.AreEqual(redirected, result.IsRedirectTo);
    }

    [TestMethod]
    public void NonLaunchActivationDoesNotBecomeProtocolFromProcessArguments()
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.File };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri]);

        Assert.AreEqual(HutaoActivationKind.None, result.Kind);
        Assert.IsNull(result.ProtocolActivatedUri);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NotificationPayloadAndUserInputAreNotReplacedByProcessArguments(bool redirected)
    {
        AppActivationArguments args = new()
        {
            Kind = ExtendedActivationKind.AppNotification,
            Arguments = "Action=LaunchGame",
            NotificationArguments = new Dictionary<string, string> { ["Action"] = "LaunchGame", ["Uid"] = "123456789" },
            UserInput = new Dictionary<string, string> { ["Reply"] = "kept" },
        };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri], redirected);

        Assert.AreEqual(HutaoActivationKind.AppNotification, result.Kind);
        Assert.AreEqual(args.Arguments, result.LaunchActivatedArguments);
        Assert.AreEqual("LaunchGame", result.AppNotificationActivatedArguments?["Action"]);
        Assert.AreEqual("123456789", result.AppNotificationActivatedArguments?["Uid"]);
        Assert.AreEqual("kept", result.AppNotificationActivatedUserInput?["Reply"]);
        Assert.AreEqual(redirected, result.IsRedirectTo);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("--ordinary-launch")]
    public void OrdinaryLaunchKeepsItsAppModelPayload(string rawLaunchArguments)
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Launch, Arguments = rawLaunchArguments };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, ["--ordinary-launch"], true);

        Assert.AreEqual(HutaoActivationKind.Launch, result.Kind);
        Assert.AreEqual(rawLaunchArguments, result.LaunchActivatedArguments);
        Assert.IsTrue(result.IsRedirectTo);
    }

    [TestMethod]
    public void UriAfterAnUnrelatedArgumentDoesNotTurnAnOrdinaryLaunchIntoProtocolActivation()
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Launch, Arguments = "--ordinary-launch" };

        HutaoActivationArguments result = HutaoActivationArguments.ResolveFromAppActivationArguments(args, ["--ordinary-launch", ForwardedUri]);

        Assert.AreEqual(HutaoActivationKind.Launch, result.Kind);
    }

    [TestMethod]
    [DataRow("hutao://[invalid")]
    [DataRow("hutao://host:invalid/")]
    public void MalformedProtocolArgumentsDoNotThrowDuringStartup(string argument)
    {
        HutaoActivationArguments? result = HutaoActivationArguments.TryCreateFromCommandLineArguments([argument]);

        Assert.AreEqual(HutaoActivationKind.Launch, result?.Kind);
        Assert.AreEqual(argument, result?.LaunchActivatedArguments);
    }

    [TestMethod]
    public void EmptyProcessArgumentsDoNotCreateAnActivation()
    {
        Assert.IsNull(HutaoActivationArguments.TryCreateFromCommandLineArguments([]));
        Assert.IsNull(HutaoActivationArguments.TryCreateFromCommandLineArguments([string.Empty]));
    }

    [TestMethod]
    public void ProtocolSchemeComparisonIsCaseInsensitive()
    {
        const string Argument = "HuTaO://launch/?uid=123456789";

        HutaoActivationArguments? result = HutaoActivationArguments.TryCreateFromCommandLineArguments([Argument]);

        Assert.AreEqual(HutaoActivationKind.Protocol, result?.Kind);
        Assert.AreEqual(Argument, result?.ProtocolActivatedUri?.OriginalString);
    }

    [TestMethod]
    public void AdministratorRestartForwardsTheResolvedNativeProtocol()
    {
        const string NativeUri = "hutao://achievement/import?value=a%2Fb%20c";
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Protocol, Uri = new(NativeUri) };
        HutaoActivationArguments resolved = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri]);

        Assert.IsTrue(resolved.TryGetAdministratorRestartArguments(out string arguments));
        Assert.AreEqual($"\"{NativeUri}\"", arguments);
    }

    [TestMethod]
    public void AdministratorRestartForwardsTheProtocolRestoredFromArgv()
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Launch };
        HutaoActivationArguments resolved = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri]);

        Assert.IsTrue(resolved.TryGetAdministratorRestartArguments(out string arguments));
        Assert.AreEqual($"\"{ForwardedUri}\"", arguments);
    }

    [TestMethod]
    public void AdministratorRestartDoesNotReplayTheRawLaunchCommandLine()
    {
        AppActivationArguments args = new() { Kind = ExtendedActivationKind.Launch, Arguments = "\"C:\\Program Files\\Snap Hutao\\app.exe\"" };
        HutaoActivationArguments resolved = HutaoActivationArguments.ResolveFromAppActivationArguments(args, []);

        Assert.IsTrue(resolved.TryGetAdministratorRestartArguments(out string arguments));
        Assert.AreEqual(string.Empty, arguments);
    }

    [TestMethod]
    public void NotificationStaysInTheRegisteredProcessInsteadOfLosingItsPayloadToElevation()
    {
        AppActivationArguments args = new()
        {
            Kind = ExtendedActivationKind.AppNotification,
            NotificationArguments = new Dictionary<string, string> { ["Action"] = "LaunchGame" },
            UserInput = new Dictionary<string, string> { ["Reply"] = "kept" },
        };
        HutaoActivationArguments resolved = HutaoActivationArguments.ResolveFromAppActivationArguments(args, [ForwardedUri]);

        Assert.IsFalse(resolved.TryGetAdministratorRestartArguments(out string arguments));
        Assert.AreEqual(string.Empty, arguments);
        Assert.AreEqual("LaunchGame", resolved.AppNotificationActivatedArguments?["Action"]);
        Assert.AreEqual("kept", resolved.AppNotificationActivatedUserInput?["Reply"]);
    }

    [TestMethod]
    [DataRow(HutaoActivationKind.None)]
    [DataRow(HutaoActivationKind.Protocol)]
    public void ActivationWithoutAForwardablePayloadDoesNotRestart(HutaoActivationKind kind)
    {
        HutaoActivationArguments args = new() { Kind = kind };

        Assert.IsFalse(args.TryGetAdministratorRestartArguments(out string arguments));
        Assert.AreEqual(string.Empty, arguments);
    }
}
