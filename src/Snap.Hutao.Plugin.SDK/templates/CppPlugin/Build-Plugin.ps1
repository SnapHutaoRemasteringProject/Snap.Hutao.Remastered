[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SdkDirectory
)
$ErrorActionPreference = 'Stop'
$SdkDirectory = (Resolve-Path -LiteralPath $SdkDirectory).Path
$root = $PSScriptRoot
$tool = Join-Path $SdkDirectory 'PluginPack\PluginPack.exe'
$native = Join-Path $SdkDirectory 'native'
if (!(Test-Path -LiteralPath $tool -PathType Leaf)) { throw "Missing PluginPack: $tool" }
& msbuild (Join-Path $root 'MyNativePlugin.vcxproj') /restore /p:Configuration=Release /p:Platform=x64 "/p:HutaoNativeSdkRoot=$native"
if ($LASTEXITCODE -ne 0) { throw "C++ build failed: $LASTEXITCODE" }
$out = Join-Path $root 'artifacts'
$stage = Join-Path $out 'stage'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$bin = Join-Path $stage 'bin\x64'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'bin\Release\MyNativePlugin.dll') -Destination $bin
$package = Join-Path $out 'MyNativePlugin.hutao'
& $tool pack (Join-Path $root 'manifest.template.json') $stage $package
if ($LASTEXITCODE -ne 0) { throw "PluginPack failed: $LASTEXITCODE" }
& $tool verify $package
if ($LASTEXITCODE -ne 0) { throw "Verification failed: $LASTEXITCODE" }
Write-Host "Plugin: $package"
