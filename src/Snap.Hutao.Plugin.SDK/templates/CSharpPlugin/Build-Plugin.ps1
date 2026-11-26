[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SdkDirectory,
    [string] $Version
)
$ErrorActionPreference = 'Stop'
$SdkDirectory = (Resolve-Path -LiteralPath $SdkDirectory).Path
$root = $PSScriptRoot
$tool = Join-Path $SdkDirectory 'PluginPack\PluginPack.exe'
$feed = Join-Path $SdkDirectory 'nuget'
if (!(Test-Path -LiteralPath $tool -PathType Leaf)) { throw "Missing PluginPack: $tool" }
if (!(Test-Path -LiteralPath $feed -PathType Container)) { throw "Missing local SDK feed: $feed" }
$out = Join-Path $root 'artifacts'
$publish = Join-Path $out 'publish'
$stage = Join-Path $out 'stage'
foreach ($dir in @($publish, $stage)) {
    if (Test-Path -LiteralPath $dir) { Remove-Item -LiteralPath $dir -Recurse -Force }
}
$project = Join-Path $root 'MyPlugin.csproj'
$args = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '-o', $publish, "-p:RestoreAdditionalProjectSources=$feed")
if ($Version) { $args += "-p:HutaoSdkVersion=$Version" }
& dotnet @args
if ($LASTEXITCODE -ne 0) { throw "Native AOT Publish failed: $LASTEXITCODE" }
$bin = Join-Path $stage 'bin\x64'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
Copy-Item -LiteralPath (Join-Path $publish 'MyPlugin.dll') -Destination $bin
$manifest = Join-Path $root 'manifest.template.json'
$package = Join-Path $out 'MyPlugin.hutao'
& $tool pack $manifest $stage $package
if ($LASTEXITCODE -ne 0) { throw "PluginPack failed: $LASTEXITCODE" }
& $tool verify $package
if ($LASTEXITCODE -ne 0) { throw "Verification failed: $LASTEXITCODE" }
Write-Host "Plugin: $package"
