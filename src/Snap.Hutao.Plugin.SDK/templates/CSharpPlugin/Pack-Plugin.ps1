[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SdkDirectory
)
$ErrorActionPreference = 'Stop'
$SdkDirectory = (Resolve-Path -LiteralPath $SdkDirectory).Path
$root = $PSScriptRoot
$tool = Join-Path $SdkDirectory 'PluginPack\PluginPack.exe'
if (!(Test-Path -LiteralPath $tool -PathType Leaf)) { throw "Missing PluginPack: $tool" }
$out = Join-Path $root 'artifacts'
$publish = Join-Path $root 'publish'
$stage = Join-Path $out 'stage'
$dll = Join-Path $publish 'MyPlugin.dll'
if (!(Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Publish MyPlugin in Visual Studio using NativeAot first: $dll" }
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$bin = Join-Path $stage 'bin\x64'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
Copy-Item -LiteralPath $dll -Destination $bin
# Optional package resources: preserve relative paths under resources/.
$resources = Join-Path $root 'resources'
if (Test-Path -LiteralPath $resources -PathType Container) {
    Get-ChildItem -LiteralPath $resources -Force | Copy-Item -Destination $stage -Recurse -Force
}
$manifest = Join-Path $root 'manifest.template.json'
$package = Join-Path $out 'MyPlugin.hutao'
& $tool pack $manifest $stage $package
if ($LASTEXITCODE -ne 0) { throw "PluginPack failed: $LASTEXITCODE" }
& $tool verify $package
if ($LASTEXITCODE -ne 0) { throw "Verification failed: $LASTEXITCODE" }
Write-Host "Plugin: $package"
