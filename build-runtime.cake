var publishAot = Argument("publishAot", true);
var publishFolder = publishAot ? "publish-aot" : "publish";
var nativeAotSymbolsDir = System.IO.Path.Combine("artifacts", "nativeaot-symbols");

Action<string, string> separateNativeAotSymbols = (repository, directory) =>
{
    // Symbols must live outside the payload directory used for MSIX, Setup and ZIP.
    var pdbPath = System.IO.Path.Combine(directory, "Snap.Hutao.Remastered.pdb");

    if (!publishAot)
    {
        if (System.IO.File.Exists(pdbPath))
        {
            System.IO.File.Delete(pdbPath);
        }
        return;
    }

    if (!System.IO.File.Exists(pdbPath))
    {
        throw new System.IO.FileNotFoundException("Native AOT symbols were not generated.", pdbPath);
    }

    var symbolsDir = System.IO.Path.Combine(repository, nativeAotSymbolsDir);
    System.IO.Directory.CreateDirectory(symbolsDir);

    var destination = System.IO.Path.Combine(symbolsDir, "Snap.Hutao.Remastered.pdb");
    System.IO.File.Copy(pdbPath, destination, true);
    System.IO.File.Delete(pdbPath);

    Information($"Native AOT symbols saved to: {destination}");
};

Func<ProcessArgumentBuilder, ProcessArgumentBuilder> withRuntimeProperties = args => args
    .Append("/p:Platform=x64")
    .Append("/p:PublishAot=" + (publishAot ? "true" : "false"))
    .Append("/p:PublishTrimmed=" + (publishAot ? "true" : "false"))
    .Append("/p:PublishReadyToRun=" + (publishAot ? "false" : "true"));

Action<string, string> verifyPublishRuntime = (repository, directory) =>
{
    var pdbPath = System.IO.Path.Combine(directory, "Snap.Hutao.Remastered.pdb");
    if (System.IO.File.Exists(pdbPath))
    {
        throw new InvalidOperationException($"Application PDB must not be packaged: {pdbPath}");
    }
    var arguments = new ProcessArgumentBuilder()
        .Append("-NoProfile").Append("-NonInteractive").Append("-File")
        .AppendQuoted(System.IO.Path.Combine(repository, "scripts", "Test-PackageRuntime.ps1"))
        .Append("-PublishDirectory").AppendQuoted(directory)
        .Append("-RuntimeKind").Append(publishAot ? "NativeAOT" : "R2R");
    var exitCode = StartProcess("pwsh", new ProcessSettings { Arguments = arguments });
    if (exitCode != 0) throw new InvalidOperationException("Publish runtime validation failed: " + exitCode);
};
