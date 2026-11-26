# Builds local SDK distribution artifacts; never pushes NuGet packages.
[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')][string] $Version = '2.0.0-preview.1',
    [switch] $IncludePrototypeHost
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts\sdk\$Version"
if (Test-Path -LiteralPath $out) { throw "SDK output already exists: $out. Remove it explicitly or choose another version." }
$nuget = Join-Path $out 'nuget'
$native = Join-Path $out 'native'
$tool = Join-Path $out 'PluginPack'
foreach ($dir in @($nuget, $native, $tool)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $LASTEXITCODE" }
}
foreach ($project in @('Snap.Hutao.Plugin.Projection', 'Snap.Hutao.Plugin.SDK')) {
    Invoke-Dotnet pack (Join-Path $root "$project\$project.csproj") -c Release -p:Platform=x64 "-p:Version=$Version" "-p:PackageVersion=$Version" -o $nuget
    $package = Join-Path $nuget "$project.$Version.nupkg"
    if (!(Test-Path -LiteralPath $package)) { throw "Missing expected package: $package" }
}
$include = Join-Path $native 'include'
$metadata = Join-Path $native 'metadata'
foreach ($dir in @($include, $metadata)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
Copy-Item -LiteralPath (Join-Path $root 'Snap.Hutao.Plugin.SDK.Native\HutaoPlugin.hpp') -Destination $include
Copy-Item -LiteralPath (Join-Path $root 'Snap.Hutao.Plugin.Contracts\HutaoPluginAbi.h') -Destination $include
Copy-Item -LiteralPath (Join-Path $root 'Snap.Hutao.Plugin.Contracts\HutaoPlugin.idl') -Destination $metadata
Copy-Item -LiteralPath (Join-Path $root 'artifacts\metadata\Snap.Hutao.Plugin.Contracts.winmd') -Destination $metadata
Compress-Archive -Path (Join-Path $native '*') -DestinationPath (Join-Path $out "Snap.Hutao.Plugin.Native.$Version.zip")
Invoke-Dotnet publish (Join-Path $root 'tools\PluginPack\PluginPack.csproj') -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:PublishSingleFile=true -o $tool
if (!(Test-Path -LiteralPath (Join-Path $tool 'PluginPack.exe'))) { throw 'PluginPack.exe missing.' }
if ($IncludePrototypeHost) {
    Invoke-Dotnet publish (Join-Path $root 'tests\PrototypeHost\PrototypeHost.csproj') -c Release -p:Platform=x64 -p:PublishProfile=JitRelease -o (Join-Path $out 'PrototypeHost')
}
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $out
$docs = Join-Path (Split-Path -Parent (Split-Path -Parent $root)) 'docs'
Copy-Item -Path (Join-Path $docs 'PluginSDK2-*.md') -Destination $out
Get-ChildItem -LiteralPath $out -Recurse -File | Where-Object { $_.Extension -in '.nupkg', '.zip', '.exe', '.winmd' } |
    Get-FileHash -Algorithm SHA256 | Format-Table -AutoSize
Write-Host "Local SDK distribution: $out"
