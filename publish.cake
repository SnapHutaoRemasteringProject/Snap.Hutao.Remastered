#addin nuget:?package=Cake.Http&version=4.0.0

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "Release");

// Paths

var repoDir = GitHubActions.IsRunningOnGitHubActions
    ? GitHubActions.Environment.Workflow.Workspace.FullPath
    : System.Environment.CurrentDirectory;

var project = System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered.csproj");
var manifest = System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Package.appxmanifest");

var binPath = System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "bin", "x64", "Release", "net10.0-windows10.0.26100.0", "win-x64");
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
    .IsDependentOn("Prepare installer output")
    .IsDependentOn("VC Redist")
    .IsDependentOn("Compile installer")
    .IsDependentOn("Sign installer");

Task("Build binary package")
    .Does(() =>
{
    Information("Building...");

    var settings = new DotNetBuildSettings
    {
        Configuration = configuration
    };

    settings.MSBuildSettings = new DotNetMSBuildSettings
    {
        ArgumentCustomization = args => args.Append("/p:Platform=x64")
                                            .Append("/p:UapAppxPackageBuildMode=SideloadOnly")
                                            .Append("/p:AppxPackageSigningEnabled=false")
                                            .Append("/p:AppxBundle=Never")
                                            .Append("/p:AppxPackageOutput=" + outputPath)
    };

    DotNetBuild(project, settings);
});

Task("Copy files")
    .IsDependentOn("Build binary package")
    .Does(() =>
{
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

    foreach (var file in files)
    {
        if (System.IO.File.Exists(file))
        {
            System.IO.File.Delete(file);
        }
    }
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
    var msix = System.IO.Path.Combine(outputPath, $"Snap.Hutao.Remastered-{version}.msix");
    var p = StartProcess(makeappx, new ProcessSettings { Arguments = $"pack /d \"{binPath}\" /p \"{msix}\"" });

    if (p != 0) { throw new InvalidOperationException($"MSIX build failed ({p})"); }
    Information($"MSIX: {msix}");
});

Task("Sign MSIX")
    .IsDependentOn("Build MSIX")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .Does(() =>
{
    var signtool = System.IO.Path.Combine(winsdkBinPath, "signtool.exe");
    var msix = System.IO.Path.Combine(outputPath, $"Snap.Hutao.Remastered-{version}.msix");
    var p = StartProcess(signtool, new ProcessSettings { Arguments = $"sign /debug /v /a /fd SHA256 /f \"{pfxPath}\" /p \"{pw}\" \"{msix}\"" });

    if (p != 0) { throw new InvalidOperationException($"MSIX sign failed ({p})"); }
});

Task("Prepare installer output")
    .IsDependentOn("Copy files")
    .Does(() =>
{
    var publishDir = System.IO.Path.Combine(repoDir, "Installer", "Publish");
    if (System.IO.Directory.Exists(publishDir)) { System.IO.Directory.Delete(publishDir, true); }
    System.IO.Directory.CreateDirectory(publishDir);
    CopyDirectory(binPath, publishDir);
});

Task("VC Redist")
    .Does(() =>
{
    var vcRedist = System.IO.Path.Combine(repoDir, "Installer", "VC_redist.x64.exe");
    if (System.IO.File.Exists(vcRedist))
    {
        Information("VC_redist.x64.exe already exists.");
        return;
    }

    Information("Downloading VC_redist.x64.exe...");
    DownloadFile("https://aka.ms/vs/17/release/vc_redist.x64.exe", vcRedist);
    Information("Downloaded VC_redist.x64.exe");
});

Task("Export code signing certificate")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .Does(() =>
{
    using (var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
        pfxPath,
        pw,
        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet))
    {
        bool isCertificateAuthority = certificate.Extensions
            .OfType<System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension>()
            .Any(extension => extension.CertificateAuthority);
        if (isCertificateAuthority)
        {
            throw new InvalidOperationException("The installer trust certificate must not be a CA certificate.");
        }

        bool supportsCodeSigning = certificate.Extensions
            .OfType<System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension>()
            .SelectMany(extension => extension.EnhancedKeyUsages.Cast<System.Security.Cryptography.Oid>())
            .Any(oid => string.Equals(oid.Value, "1.3.6.1.5.5.7.3.3", System.StringComparison.Ordinal));
        if (!supportsCodeSigning)
        {
            throw new InvalidOperationException("The installer trust certificate must have the code-signing EKU.");
        }

        codeSigningCertificateThumbprint = certificate.Thumbprint.Replace(" ", string.Empty).ToUpperInvariant();

        System.IO.File.WriteAllBytes(
            codeSigningCertificatePath,
            certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert));
        Information($"Exported code-signing certificate: {codeSigningCertificatePath} (thumbprint: {codeSigningCertificateThumbprint})");
    }
});

Task("Compile installer")
    .IsDependentOn("Prepare installer output")
    .IsDependentOn("VC Redist")
    .IsDependentOn("Export code signing certificate")
    .Does(() =>
{
    var iscc = Context.Tools.Resolve("iscc.exe")?.FullPath;
    if (string.IsNullOrEmpty(iscc))
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        iscc = System.IO.Directory.GetDirectories(pf, "Inno Setup*")
            .Select(d => System.IO.Path.Combine(d, "iscc.exe"))
            .FirstOrDefault(System.IO.File.Exists);
    }

    if (string.IsNullOrEmpty(iscc)) { throw new Exception("Inno Setup not found"); }

    var iss = System.IO.Path.Combine(repoDir, "Installer", "installer.iss");
    var codeSigningCertificatePathArgument = System.IO.File.Exists(codeSigningCertificatePath)
        ? $"/d{CodeSigningCertificatePathArgumentName}=\"{codeSigningCertificatePath}\" "
        : string.Empty;
    var codeSigningCertificateThumbprintArgument = !string.IsNullOrEmpty(codeSigningCertificateThumbprint)
        ? $"/d{CodeSigningCertificateThumbprintArgumentName}=\"{codeSigningCertificateThumbprint}\" "
        : string.Empty;
    var p = StartProcess(iscc, new ProcessSettings { Arguments = $"/dMyAppVersion=\"{version}\" {codeSigningCertificatePathArgument}{codeSigningCertificateThumbprintArgument}\"{iss}\"", WorkingDirectory = repoDir });

    if (p != 0) { throw new InvalidOperationException($"Inno Setup failed ({p})"); }
    Information("Installer compiled.");
});

Task("Sign installer")
    .IsDependentOn("Compile installer")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .Does(() =>
{
    var signtool = Context.Tools.Resolve("signtool.exe")?.FullPath
        ?? System.IO.Path.Combine(winsdkBinPath, "signtool.exe");

    if (!System.IO.File.Exists(signtool)) { Information("signtool not found, skipping."); return; }

    var installerDir = System.IO.Path.Combine(repoDir, "publish");
    foreach (var installer in System.IO.Directory.GetFiles(installerDir, "Snap.Hutao.Remastered-*.exe"))
    {
        var p = StartProcess(signtool, new ProcessSettings { Arguments = $"sign /debug /v /a /fd SHA256 /f \"{pfxPath}\" /p \"{pw}\" /tr http://timestamp.digicert.com /td SHA256 \"{installer}\"" });
        Information(p == 0 ? $"Signed: {System.IO.Path.GetFileName(installer)}" : $"Failed: {System.IO.Path.GetFileName(installer)}");
    }
});

RunTarget(target);
