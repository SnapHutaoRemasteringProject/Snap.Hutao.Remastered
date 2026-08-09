#addin nuget:?package=Cake.Http&version=4.0.0

var target = Argument("target", "Build");
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

// SignPath test certificate subject
const string TestCertificateSubject = "CN=Test certificate for 'Snap.Hutao.Remastered [OSS]'";

// ============================================================
// Tasks
// ============================================================

Task("Build")
    .IsDependentOn("Modify manifest for test signing")
    .IsDependentOn("Build binary package")
    .IsDependentOn("Copy files")
    .IsDependentOn("Remove unused files")
    .IsDependentOn("Build MSIX");

Task("Modify manifest for test signing")
    .Does(() =>
{
    Information("Modifying manifest for SignPath test signing...");

    var content = System.IO.File.ReadAllText(manifest);

    // Replace publisher with SignPath test certificate subject
    content = System.Text.RegularExpressions.Regex.Replace(
        content,
        "  Publisher=\"([^\"]*)\"",
        $"  Publisher=\"{TestCertificateSubject}\"");

    // Replace display names to indicate test build
    content = content
        .Replace("Snap Hutao Remastered", "Snap Hutao Remastered Test")
        .Replace("胡桃重制版", "胡桃重制版 Test");

    System.IO.File.WriteAllText(manifest, content);

    Information("Manifest updated for test signing.");
});

Task("Build binary package")
    .IsDependentOn("Modify manifest for test signing")
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

Task("Build MSIX")
    .IsDependentOn("Build binary package")
    .IsDependentOn("Copy files")
    .IsDependentOn("Remove unused files")
    .Does(() =>
{
    var makeappx = System.IO.Path.Combine(winsdkBinPath, "makeappx.exe");
    var msix = System.IO.Path.Combine(outputPath, $"Snap.Hutao.Remastered.Test-{version}.msix");
    var p = StartProcess(makeappx, new ProcessSettings { Arguments = $"pack /d \"{binPath}\" /p \"{msix}\"" });

    if (p != 0) { throw new InvalidOperationException($"MSIX build failed ({p})"); }
    Information($"Unsigned MSIX: {msix}");

    if (GitHubActions.IsRunningOnGitHubActions)
    {
        GitHubActions.Commands.SetOutputParameter("msix-path", msix);
    }
});

RunTarget(target);
