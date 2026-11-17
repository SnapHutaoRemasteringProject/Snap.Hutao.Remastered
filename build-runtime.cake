// Shared by every packaging entry point. Keep the normal build explicitly R2R;
// AOT is a separate CI leg and must use matching restore/publish properties.
var publishAot = Argument("publishAot", false);
var artifactSuffix = publishAot ? "-AOT" : "";
var installerFlavorArgument = publishAot ? "/dBuildFlavorSuffix=\"-AOT\" " : "";
var publishFolder = publishAot ? "publish-aot" : "publish";

Func<ProcessArgumentBuilder, ProcessArgumentBuilder> withRuntimeProperties = args => args
    .Append("/p:Platform=x64")
    .Append("/p:PublishAot=" + (publishAot ? "true" : "false"))
    .Append("/p:PublishTrimmed=" + (publishAot ? "true" : "false"))
    .Append("/p:PublishReadyToRun=" + (publishAot ? "false" : "true"));

Action<string, string> verifyPublishRuntime = (repository, directory) =>
{
    var arguments = new ProcessArgumentBuilder()
        .Append("-NoProfile").Append("-NonInteractive").Append("-File")
        .AppendQuoted(System.IO.Path.Combine(repository, "scripts", "Test-PackageRuntime.ps1"))
        .Append("-PublishDirectory").AppendQuoted(directory)
        .Append("-RuntimeKind").Append(publishAot ? "NativeAOT" : "R2R");
    var exitCode = StartProcess("pwsh", new ProcessSettings { Arguments = arguments });
    if (exitCode != 0) throw new InvalidOperationException("Publish runtime validation failed: " + exitCode);
};
