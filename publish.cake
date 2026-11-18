#load "./build-runtime.cake"

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "Release");

// Paths

var repoDir = GitHubActions.IsRunningOnGitHubActions
    ? GitHubActions.Environment.Workflow.Workspace.FullPath
    : System.Environment.CurrentDirectory;

var project = System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered.csproj");
var manifest = System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Package.appxmanifest");

var targetFramework = XmlPeek(project, "/Project/PropertyGroup/TargetFramework/text()").Trim();
var runtimeIdentifier = XmlPeek(project, "/Project/PropertyGroup/RuntimeIdentifier/text()").Trim();
var binPath = System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "bin", "x64", configuration, targetFramework, runtimeIdentifier, publishFolder);
var outputPath = System.IO.Path.Combine(repoDir, "src", "output");

// Version: read from Package.appxmanifest
var version = XmlPeek(manifest, "appx:Package/appx:Identity/@Version", new XmlPeekSettings
{
    Namespaces = new Dictionary<string, string> { { "appx", "http://schemas.microsoft.com/appx/manifest/foundation/windows10" } }
});

Information($"Version: {version}");

// Certificate (CI only)

var pfxPath = "pfxPath";
var pw = "pw";
string codeSigningCertificateThumbprint = "";
const string CodeSigningCertificatePathArgumentName = "CodeSigningCertificatePath";
const string CodeSigningCertificateThumbprintArgumentName = "CodeSigningCertificateThumbprint";
var codeSigningCertificatePath = System.IO.Path.Combine(repoDir, "temp-code-signing.cer");

if (GitHubActions.IsRunningOnGitHubActions)
{
    var certificateBase64 = HasEnvironmentVariable("PUBLISH_CERT") ? EnvironmentVariable("PUBLISH_CERT") : throw new Exception("Cannot find PUBLISH_CERT");
    pw = HasEnvironmentVariable("PUBLISH_PW") ? EnvironmentVariable("PUBLISH_PW") : throw new Exception("Cannot find PUBLISH_PW");
    pfxPath = System.IO.Path.Combine(repoDir, "temp.pfx");
    System.IO.File.WriteAllBytes(pfxPath, System.Convert.FromBase64String(certificateBase64));

    GitHubActions.Commands.SetOutputParameter("version", version);
}

// Windows SDK

var winsdkRegistry = new WindowsRegistry().LocalMachine.OpenKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots");
var winsdkVersion = winsdkRegistry.GetSubKeyNames().MaxBy(key => int.Parse(key.Split(".")[2]));
var winsdkPath = (string)winsdkRegistry.GetValue("KitsRoot10");
var winsdkBinPath = System.IO.Path.Combine(winsdkPath, "bin", winsdkVersion, "x64");
Information($"Windows SDK: {winsdkPath}");

// ============================================================
// Tasks
// ============================================================

Task("Publish")
    .IsDependentOn("Build binary package")
    .IsDependentOn("Copy files")
    .IsDependentOn("Remove unused files")
    .IsDependentOn("Inner Sign")
    .IsDependentOn("Build MSIX")
    .IsDependentOn("Sign MSIX")
    .IsDependentOn("Build AppPackages");

Task("Build binary package")
    .Does(() =>
{
    Information("Building...");

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = runtimeIdentifier,
        OutputDirectory = binPath
    };

    settings.MSBuildSettings = new DotNetMSBuildSettings
    {
        ArgumentCustomization = args => withRuntimeProperties(args)
                                            .Append("/p:UapAppxPackageBuildMode=SideloadOnly")
                                            .Append("/p:AppxPackageSigningEnabled=false")
                                            .Append("/p:AppxBundle=Never")
                                            .Append("/p:AppxPackageOutput=" + outputPath)
    };

    DotNetPublish(project, settings);
});

Task("Copy files")
    .IsDependentOn("Build binary package")
    .Does(() =>
{
    CopyFileToDirectory(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(binPath), "AppxManifest.xml"), binPath);
    CopyDirectory(
        System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Assets"),
        System.IO.Path.Combine(binPath, "Assets"));

    CopyDirectory(
        System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Resource"),
        System.IO.Path.Combine(binPath, "Resource"));

    Information("Assets and resource copied.");
});

Task("Remove unused files")
    .IsDependentOn("Build binary package")
    .Does(() =>
{
    var files = new[]
    {
        System.IO.Path.Combine(binPath, "App.xbf"),
        System.IO.Path.Combine(binPath, "Snap.Hutao.Remastered.build.appxrecipe"),
        System.IO.Path.Combine(binPath, "onnxruntime.dll"),
    };

    foreach (var file in files.Where(file => !publishAot || System.IO.Path.GetFileName(file) != "App.xbf"))
    {
        if (System.IO.File.Exists(file))
        {
            System.IO.File.Delete(file);
        }
    }
    separateNativeAotSymbols(repoDir, binPath);
    verifyPublishRuntime(repoDir, binPath);
});

Task("Inner Sign")
    .IsDependentOn("Build binary package")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .Does(() =>
{
    var signtool = System.IO.Path.Combine(winsdkBinPath, "signtool.exe");
    var p = StartProcess(
        signtool,
        new ProcessSettings { Arguments = $"sign /debug /v /as /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /f \"{pfxPath}\" /p \"{pw}\" \"{System.IO.Path.Combine(binPath, "*.exe")}\" \"{System.IO.Path.Combine(binPath, "*.dll")}\"" });

    if (p != 0) { throw new InvalidOperationException($"Inner sign failed ({p})"); }
});

Task("Build MSIX")
    .IsDependentOn("Build binary package")
    .IsDependentOn("Copy files")
    .IsDependentOn("Remove unused files")
    .IsDependentOn("Inner Sign")
    .Does(() =>
{
    var makeappx = System.IO.Path.Combine(winsdkBinPath, "makeappx.exe");
    var msix = System.IO.Path.Combine(outputPath, $"Snap.Hutao.Remastered-{version}{artifactSuffix}.msix");
    var p = StartProcess(makeappx, new ProcessSettings { Arguments = $"pack /d \"{binPath}\" /p \"{msix}\"" });

    if (p != 0) { throw new InvalidOperationException($"MSIX build failed ({p})"); }
    Information($"MSIX: {msix}");
});

Task("Sign MSIX")
    .IsDependentOn("Build MSIX")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .Does(() =>
{

    var packageManifest = new System.Xml.XmlDocument();
    packageManifest.Load(System.IO.Path.Combine(binPath, "AppxManifest.xml"));
    var packagePublisher = packageManifest.DocumentElement.SelectSingleNode("*[local-name()='Identity']").Attributes["Publisher"].Value;
    using (var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
        pfxPath, pw, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet))
    {
        if (!string.Equals(certificate.Subject, packagePublisher, System.StringComparison.Ordinal))
        {
            if (packagePublisher != "CN=469D671A-3AED-4488-AC4F-75E5394BC7FA")
            {
                throw new InvalidOperationException("MSIX Publisher does not match the signing certificate.");
            }
            Warning("Store MSIX retains its Store Publisher and is UNSIGNED. It is for Store submission, not sideload installation. Setup signing is unchanged.");
            GitHubActions.Commands.SetOutputParameter("msix-signed", "false");
            return;
        }
    }
    GitHubActions.Commands.SetOutputParameter("msix-signed", "true");
    var signtool = System.IO.Path.Combine(winsdkBinPath, "signtool.exe");
    var msix = System.IO.Path.Combine(outputPath, $"Snap.Hutao.Remastered-{version}{artifactSuffix}.msix");
    var p = StartProcess(signtool, new ProcessSettings { Arguments = $"sign /debug /v /a /fd SHA256 /f \"{pfxPath}\" /p \"{pw}\" \"{msix}\"" });

    if (p != 0) { throw new InvalidOperationException($"MSIX sign failed ({p})"); }
});

Task("Build AppPackages")
    .IsDependentOn("Sign MSIX")
    // Preserve the Store package; only R2R gets the separate, signed R2 package.
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions && !publishAot)
    .Does(() =>
{
    var arguments = new ProcessArgumentBuilder()
        .Append("-NoProfile").Append("-NonInteractive").Append("-File")
        .AppendQuoted(System.IO.Path.Combine(repoDir, "scripts", "New-AppInstallerPackages.ps1"))
        .Append("-PublishDirectory").AppendQuoted(binPath)
        .Append("-AppPackagesDirectory").AppendQuoted(System.IO.Path.Combine(repoDir, "AppPackages"))
        .Append("-StagingDirectory").AppendQuoted(System.IO.Path.Combine(repoDir, "artifacts", "appinstaller-staging"))
        .Append("-CertificatePath").AppendQuoted(pfxPath)
        .Append("-MakeAppxPath").AppendQuoted(System.IO.Path.Combine(winsdkBinPath, "makeappx.exe"))
        .Append("-SignToolPath").AppendQuoted(System.IO.Path.Combine(winsdkBinPath, "signtool.exe"));
    var result = StartProcess("pwsh", new ProcessSettings { Arguments = arguments });
    if (result != 0)
    {
        throw new InvalidOperationException("R2 AppPackages generation or signing failed (exit " + result + ").");
    }
});

RunTarget(target);
