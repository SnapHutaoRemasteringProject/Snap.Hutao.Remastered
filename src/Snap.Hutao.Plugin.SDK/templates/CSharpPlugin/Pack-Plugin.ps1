[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SdkDirectory
)
$ErrorActionPreference = 'Stop'
$SdkDirectory = (Resolve-Path -LiteralPath $SdkDirectory).Path
$root = $PSScriptRoot
$manifest = Join-Path $root 'manifest.template.json'
$entry = (Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json).runtime.entry
$pluginName = [IO.Path]::GetFileNameWithoutExtension($entry)
$tool = Join-Path $SdkDirectory 'PluginPack\PluginPack.exe'
if (!(Test-Path -LiteralPath $tool -PathType Leaf)) { throw "Missing PluginPack: $tool" }
$out = Join-Path $root 'artifacts'
$stage = Join-Path $out 'stage'
$dll = Join-Path $root ('publish\{0}.dll' -f $pluginName)
if (!(Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Publish the project in Visual Studio using NativeAot first: $dll" }
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
