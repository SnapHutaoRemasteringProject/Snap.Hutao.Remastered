#tool "nuget:?package=nuget.commandline&version=6.9.1"
#addin nuget:?package=Cake.Http&version=4.0.0

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");

// Pre-define

var version = "0.0.0.0";

var repoDir = "repoDir";

var pfxPath = "pfxPath";
var pw = "pw";
string codeSigningCertificateThumbprint = "";
const string CodeSigningCertificatePathArgumentName = "CodeSigningCertificatePath";
const string CodeSigningCertificateThumbprintArgumentName = "CodeSigningCertificateThumbprint";

// Properties

string project
{
    get => System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered.csproj");
}
string binPath
{
    get => System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "bin", "x64", "Release", "net10.0-windows10.0.26100.0", "win-x64");
}
string installerPublishDir
{
    get => System.IO.Path.Combine(repoDir, "Installer", "Publish");
}
string installerOutputDir
{
    get => System.IO.Path.Combine(repoDir, "publish");
}
string vcRedistPath
{
    get => System.IO.Path.Combine(repoDir, "Installer", "VC_redist.x64.exe");
}
string issFile
{
    get => System.IO.Path.Combine(repoDir, "Installer", "installer.iss");
}
string codeSigningCertificatePath
{
    get => System.IO.Path.Combine(repoDir, "temp-code-signing.cer");
}

var isPullRequest =
    GitHubActions.IsRunningOnGitHubActions &&
    GitHubActions.Environment.PullRequest.IsPullRequest;

if (GitHubActions.IsRunningOnGitHubActions)
{
    repoDir = GitHubActions.Environment.Workflow.Workspace.FullPath;

    // Use date-based version same as build.cake
    version = System.DateTime.Now.ToString("yyyy.M.d.") + ((int)((System.DateTime.Now - System.DateTime.Today).TotalSeconds / 86400 * 65535)).ToString();

    GitHubActions.Commands.SetOutputParameter("version", version);
    Information($"Version: {version}");
}
else // Local
{
    repoDir = System.Environment.CurrentDirectory;

    // Extract version from installer.iss
    var issContent = System.IO.File.ReadAllText(issFile);
    var match = System.Text.RegularExpressions.Regex.Match(issContent, @"#define\s+MyAppVersion\s+""([^""]+)""");
    if (match.Success)
    {
        version = match.Groups[1].Value;
    }
    else
    {
        version = System.DateTime.Now.ToString("yyyy.M.d.") + ((int)((System.DateTime.Now - System.DateTime.Today).TotalSeconds / 86400 * 65535)).ToString();
    }

    Information($"Version: {version}");
}

if (GitHubActions.IsRunningOnGitHubActions && !isPullRequest)
{
    var certificateBase64 = HasEnvironmentVariable("CERTIFICATE") ? EnvironmentVariable("CERTIFICATE") : throw new Exception("Cannot find CERTIFICATE");
    pw = HasEnvironmentVariable("PW") ? EnvironmentVariable("PW") : throw new Exception("Cannot find PW");
    pfxPath = System.IO.Path.Combine(repoDir, "temp.pfx");
    System.IO.File.WriteAllBytes(pfxPath, System.Convert.FromBase64String(certificateBase64));
}

// Windows SDK (for signtool.exe)
string winsdkBinPath = "";
try
{
    var registry = new WindowsRegistry();
    var winsdkRegistry = registry.LocalMachine.OpenKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots");
    var winsdkVersion = winsdkRegistry.GetSubKeyNames().MaxBy(key => int.Parse(key.Split(".")[2]));
    var winsdkPath = (string)winsdkRegistry.GetValue("KitsRoot10");
    winsdkBinPath = System.IO.Path.Combine(winsdkPath, "bin", winsdkVersion, "x64");
    Information($"Windows SDK: {winsdkPath}");
}
catch
{
    Information("Windows SDK not found, will try PATH for signtool.");
}

Task("Build")
    .IsDependentOn("NuGet Restore")
    .IsDependentOn("Build binary package")
    .IsDependentOn("Copy files")
    .IsDependentOn("Remove unused files")
    .IsDependentOn("Prepare installer output")
    .IsDependentOn("VC Redist")
    .IsDependentOn("Compile installer")
    .IsDependentOn("Sign installer");

Task("NuGet Restore")
    .Does(() =>
{
    Information("Restoring packages...");

    var nugetConfig = System.IO.Path.Combine(repoDir, "NuGet.Config");
    DotNetRestore(project, new DotNetRestoreSettings
    {
        Verbosity = DotNetVerbosity.Detailed,
        Interactive = false,
        ConfigFile = nugetConfig
    });
});

Task("Build binary package")
    .IsDependentOn("NuGet Restore")
    .Does(() =>
{
    Information("Building binary package...");

    var settings = new DotNetBuildSettings
    {
        Configuration = configuration
    };

    settings.MSBuildSettings = new DotNetMSBuildSettings
    {
        ArgumentCustomization = args => args.Append("/p:Platform=x64")
                                            .Append("/p:AppxPackageSigningEnabled=false")
                                            .Append("/p:AppxBundle=Never")
    };

    DotNetBuild(project, settings);
});

Task("Copy files")
    .IsDependentOn("Build binary package")
    .Does(() =>
{
    Information("Copying assets...");
    CopyDirectory(
        System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Assets"),
        System.IO.Path.Combine(binPath, "Assets")
    );

    Information("Copying resource...");
    CopyDirectory(
        System.IO.Path.Combine(repoDir, "src", "Snap.Hutao.Remastered", "Snap.Hutao.Remastered", "Resource"),
        System.IO.Path.Combine(binPath, "Resource")
    );
});

Task("Remove unused files")
    .IsDependentOn("Build binary package")
    .Does(() =>
{
    Information("Removing unused files...");

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

Task("Prepare installer output")
    .IsDependentOn("Copy files")
    .Does(() =>
{
    Information("Preparing installer publish directory...");

    // Clean and recreate
    if (System.IO.Directory.Exists(installerPublishDir))
    {
        System.IO.Directory.Delete(installerPublishDir, true);
    }
    System.IO.Directory.CreateDirectory(installerPublishDir);

    // Copy build output to installer publish dir
    CopyDirectory(binPath, installerPublishDir);
});

Task("VC Redist")
    .Does(() =>
{
    if (!System.IO.File.Exists(vcRedistPath))
    {
        Information("Downloading VC_redist.x64.exe...");
        try
        {
            DownloadFile("https://aka.ms/vs/17/release/vc_redist.x64.exe", vcRedistPath);
            Information("Downloaded successfully.");
        }
        catch (Exception ex)
        {
            Information($"Failed to download VC_redist.x64.exe: {ex.Message}");
            Information("The installer may not include the VC++ runtime.");
        }
    }
    else
    {
        Information("VC_redist.x64.exe already present, skipping download.");
    }
});

Task("Export code signing certificate")
    .Does(() =>
{
    if (!GitHubActions.IsRunningOnGitHubActions || isPullRequest)
    {
        Information("Local or pull-request configuration. Skip code-signing certificate export.");
        return;
    }

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
    Information("Compiling installer with Inno Setup...");

    var isccPath = "";
    var iscc = Context.Tools.Resolve("iscc.exe");
    if (iscc != null)
    {
        isccPath = iscc.FullPath;
    }
    else
    {
        // Search common installation paths
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var innoDirs = System.IO.Directory.GetDirectories(programFiles, "Inno Setup*");
        foreach (var dir in innoDirs)
        {
            var candidate = System.IO.Path.Combine(dir, "iscc.exe");
            if (System.IO.File.Exists(candidate))
            {
                isccPath = candidate;
                break;
            }
        }
    }

    if (string.IsNullOrEmpty(isccPath))
    {
        throw new Exception("Inno Setup (iscc.exe) not found. Install from: https://jrsoftware.org/isinfo.php");
    }

    Information($"Using iscc: {isccPath}");

    var codeSigningCertificatePathArgument = System.IO.File.Exists(codeSigningCertificatePath)
        ? $"/d{CodeSigningCertificatePathArgumentName}=\"{codeSigningCertificatePath}\" "
        : string.Empty;
    var codeSigningCertificateThumbprintArgument = !string.IsNullOrEmpty(codeSigningCertificateThumbprint)
        ? $"/d{CodeSigningCertificateThumbprintArgumentName}=\"{codeSigningCertificateThumbprint}\" "
        : string.Empty;
    var p = StartProcess(
        isccPath,
        new ProcessSettings
        {
            Arguments = $"/dMyAppVersion=\"{version}\" {codeSigningCertificatePathArgument}{codeSigningCertificateThumbprintArgument}\"{issFile}\"",
            WorkingDirectory = repoDir
        }
    );

    if (p != 0)
    {
        throw new InvalidOperationException("Inno Setup compilation failed with exit code " + p);
    }

    Information("Installer compiled successfully.");
    Information($"Output directory: {installerOutputDir}");
});

Task("Sign installer")
    .IsDependentOn("Compile installer")
    .Does(() =>
{
    if (GitHubActions.IsRunningOnGitHubActions)
    {
        if (!System.IO.File.Exists(pfxPath))
        {
            Information("Certificate not found, skipping installer signing.");
            return;
        }

        var signTool = Context.Tools.Resolve("signtool.exe");
        var signToolPath = signTool?.FullPath;

        if (string.IsNullOrEmpty(signToolPath))
        {
            // Fallback to Windows SDK path
            if (!string.IsNullOrEmpty(winsdkBinPath))
            {
                signToolPath = System.IO.Path.Combine(winsdkBinPath, "signtool.exe");
            }
        }

        if (string.IsNullOrEmpty(signToolPath) || !System.IO.File.Exists(signToolPath))
        {
            Information("signtool.exe not found, skipping installer signing.");
            return;
        }

        Information($"Using signtool: {signToolPath}");

        // Find the generated installer
        var installerFiles = System.IO.Directory.GetFiles(installerOutputDir, "Snap.Hutao.Remastered-*.exe");
        foreach (var installer in installerFiles)
        {
            Information($"Signing installer: {System.IO.Path.GetFileName(installer)}...");
            var p = StartProcess(
                signToolPath,
                new ProcessSettings
                {
                    Arguments = $"sign /debug /v /a /fd SHA256 /f \"{pfxPath}\" /p \"{pw}\" /tr http://timestamp.digicert.com /td SHA256 \"{installer}\""
                }
            );
            if (p != 0)
            {
                Information($"Failed to sign installer: {installer}");
            }
            else
            {
                Information($"Signed: {System.IO.Path.GetFileName(installer)}");
            }
        }
    }
    else
    {
        Information("Local configuration. Skip signing.");
    }
});

RunTarget(target);
