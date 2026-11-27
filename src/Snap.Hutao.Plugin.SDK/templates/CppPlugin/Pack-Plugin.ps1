[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SdkDirectory,
    [ValidateSet('Debug','Release')][string] $Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$SdkDirectory = (Resolve-Path -LiteralPath $SdkDirectory).Path
$root = $PSScriptRoot
$manifest = Join-Path $root 'manifest.template.json'
$entry = (Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json).runtime.entry
$pluginName = [IO.Path]::GetFileNameWithoutExtension($entry)
$tool = Join-Path $SdkDirectory 'PluginPack\PluginPack.exe'
if (!(Test-Path -LiteralPath $tool -PathType Leaf)) { throw "Missing PluginPack: $tool" }
$dll = Join-Path $root "bin\$Configuration\$pluginName.dll"
if (!(Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Build the project in Visual Studio ($Configuration|x64) first: $dll" }
$out = Join-Path $root 'artifacts'
$stage = Join-Path $out 'stage'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$bin = Join-Path $stage 'bin\x64'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
Copy-Item -LiteralPath $dll -Destination $bin
# Optional package resources: preserve relative paths under resources/.
$resources = Join-Path $root 'resources'
if (Test-Path -LiteralPath $resources -PathType Container) {
    Copy-Item -LiteralPath $resources -Destination $stage -Recurse -Force
}
$package = Join-Path $out "$pluginName.hutao"
& $tool pack $manifest $stage $package
if ($LASTEXITCODE -ne 0) { throw "PluginPack failed: $LASTEXITCODE" }
& $tool verify $package
if ($LASTEXITCODE -ne 0) { throw "Verification failed: $LASTEXITCODE" }
Write-Host "Plugin: $package"
