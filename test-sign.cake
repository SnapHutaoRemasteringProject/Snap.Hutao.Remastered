#addin nuget:?package=Cake.Http&version=4.0.0

var target = Argument("target", "BuildAndZip");
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

if (GitHubActions.IsRunningOnGitHubActions)
{
    GitHubActions.Commands.SetOutputParameter("version", version);
}

// Windows SDK

var winsdkRegistry = new WindowsRegistry().LocalMachine.OpenKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots");
var winsdkVersion = winsdkRegistry.GetSubKeyNames().MaxBy(key => int.Parse(key.Split(".")[2]));
var winsdkPath = (string)winsdkRegistry.GetValue("KitsRoot10");
var winsdkBinPath = System.IO.Path.Combine(winsdkPath, "bin", winsdkVersion, "x64");
Information($"Windows SDK: {winsdkPath}");

var zipPath = System.IO.Path.Combine(outputPath, $"Snap.Hutao.Remastered.Test-{version}-unsigned.zip");

// ============================================================
// BuildAndZip (default target): build project and zip loose files
// ============================================================

Task("BuildAndZip")
    .IsDependentOn("Build binary package")
    .IsDependentOn("Copy files")
    .IsDependentOn("Remove unused files")
    .IsDependentOn("Zip loose files");

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

Task("Zip loose files")
    .IsDependentOn("Build binary package")
    .IsDependentOn("Copy files")
    .IsDependentOn("Remove unused files")
    .Does(() =>
{
    if (!System.IO.Directory.Exists(outputPath))
    {
        System.IO.Directory.CreateDirectory(outputPath);
    }

    if (System.IO.File.Exists(zipPath))
    {
        System.IO.File.Delete(zipPath);
    }

    System.IO.Compression.ZipFile.CreateFromDirectory(binPath, zipPath);
    Information($"Unsigned zip: {zipPath}");

    if (GitHubActions.IsRunningOnGitHubActions)
    {
        GitHubActions.Commands.SetOutputParameter("zip-path", zipPath);
    }
});

// ============================================================
// PackageFromSigned: extract signed zip, build installer + MSIX
// ============================================================

var signedZipDir = Argument<string>("signedZipDir", null);

Task("PackageFromSigned")
    .IsDependentOn("Extract signed zip")
    .IsDependentOn("Prepare installer output")
    .IsDependentOn("VC Redist")
    .IsDependentOn("Compile installer")
    .IsDependentOn("Build MSIX");

Task("Extract signed zip")
    .Does(() =>
{
    if (string.IsNullOrEmpty(signedZipDir))
    {
        throw new Exception("--signedZipDir argument is required for PackageFromSigned target");
    }

    var signedZip = System.IO.Directory.GetFiles(signedZipDir, "*.zip").FirstOrDefault();
    if (signedZip == null)
    {
        throw new Exception($"No signed zip found in: {signedZipDir}");
    }

    Information($"Extracting signed zip: {signedZip}");

    if (System.IO.Directory.Exists(binPath))
    {
        System.IO.Directory.Delete(binPath, true);
    }

    System.IO.Compression.ZipFile.ExtractToDirectory(signedZip, binPath);
    Information($"Extracted to: {binPath}");
});

Task("Prepare installer output")
    .IsDependentOn("Extract signed zip")
    .Does(() =>
{
    var publishDir = System.IO.Path.Combine(repoDir, "Installer", "Publish");
    if (System.IO.Directory.Exists(publishDir))
    {
        System.IO.Directory.Delete(publishDir, true);
    }

    System.IO.Directory.CreateDirectory(publishDir);
    CopyDirectory(binPath, publishDir);
    Information("Installer publish directory prepared.");
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

Task("Compile installer")
    .IsDependentOn("Prepare installer output")
    .IsDependentOn("VC Redist")
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
    var p = StartProcess(iscc, new ProcessSettings { Arguments = $"/dMyAppVersion=\"{version}\" \"{iss}\"", WorkingDirectory = repoDir });

    if (p != 0) { throw new InvalidOperationException($"Inno Setup failed ({p})"); }
    Information("Installer compiled.");

    if (GitHubActions.IsRunningOnGitHubActions)
    {
        var installerDir = System.IO.Path.Combine(repoDir, "publish");
        var installer = System.IO.Directory.GetFiles(installerDir, "Snap.Hutao.Remastered-*.exe").FirstOrDefault();
        if (installer != null)
        {
            GitHubActions.Commands.SetOutputParameter("installer-path", installer);
        }
    }
});

Task("Build MSIX")
    .IsDependentOn("Extract signed zip")
    .Does(() =>
{
    var makeappx = System.IO.Path.Combine(winsdkBinPath, "makeappx.exe");
    var msix = System.IO.Path.Combine(outputPath, $"Snap.Hutao.Remastered.Test-{version}.msix");
    var p = StartProcess(makeappx, new ProcessSettings { Arguments = $"pack /d \"{binPath}\" /p \"{msix}\"" });

    if (p != 0) { throw new InvalidOperationException($"MSIX build failed ({p})"); }
    Information($"MSIX (unsigned): {msix}");

    if (GitHubActions.IsRunningOnGitHubActions)
    {
        GitHubActions.Commands.SetOutputParameter("msix-path", msix);
    }
});

RunTarget(target);
