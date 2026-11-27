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
    Invoke-Dotnet pack (Join-Path $root "libraries\$project\$project.csproj") -c Release -p:Platform=x64 "-p:Version=$Version" "-p:PackageVersion=$Version" -o $nuget
    $package = Join-Path $nuget "$project.$Version.nupkg"
    if (!(Test-Path -LiteralPath $package)) { throw "Missing expected package: $package" }
}
$include = Join-Path $native 'include'
$metadata = Join-Path $native 'metadata'
foreach ($dir in @($include, $metadata)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
Copy-Item -LiteralPath (Join-Path $root 'libraries\Snap.Hutao.Plugin.SDK.Native\HutaoPlugin.hpp') -Destination $include
Copy-Item -LiteralPath (Join-Path $root 'contracts\HutaoPluginAbi.h') -Destination $include
Copy-Item -LiteralPath (Join-Path $root 'contracts\HutaoPlugin.idl') -Destination $metadata
Copy-Item -LiteralPath (Join-Path $root 'artifacts\metadata\Snap.Hutao.Plugin.Contracts.winmd') -Destination $metadata
Copy-Item -LiteralPath (Join-Path $root 'artifacts\metadata\Snap.Hutao.Plugin.Services.winmd') -Destination $metadata
Compress-Archive -Path (Join-Path $native '*') -DestinationPath (Join-Path $out "Snap.Hutao.Plugin.Native.$Version.zip")
Invoke-Dotnet publish (Join-Path $root 'tools\PluginPack\PluginPack.csproj') -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:PublishSingleFile=true -o $tool
if (!(Test-Path -LiteralPath (Join-Path $tool 'PluginPack.exe'))) { throw 'PluginPack.exe missing.' }
if ($IncludePrototypeHost) {
    Invoke-Dotnet publish (Join-Path $root 'tests\PrototypeHost\PrototypeHost.csproj') -c Release -p:Platform=x64 -p:PublishProfile=JitRelease -o (Join-Path $out 'PrototypeHost')
}
$docs = Join-Path (Split-Path -Parent (Split-Path -Parent $root)) 'docs'
$docOut = Join-Path $out 'docs'
New-Item -ItemType Directory -Path $docOut | Out-Null
Copy-Item -Path (Join-Path $docs 'PluginSDK2-*.md') -Destination $docOut
Copy-Item -LiteralPath (Join-Path $root 'templates') -Destination $out -Recurse
$csproj = Join-Path $out 'templates\CSharpPlugin\MyPlugin.csproj'
(Get-Content -LiteralPath $csproj -Raw).Replace('>2.0.0-preview.1</HutaoSdkVersion>', ">$Version</HutaoSdkVersion>") | Set-Content -LiteralPath $csproj -Encoding utf8
@"
# Snap Hutao Plugin SDK $Version

- Architecture and development model: docs/PluginSDK2-Architecture.md
- C# First business service authoring: docs/PluginSDK2-Authoring.md
- Developer guide: docs/PluginSDK2-GettingStarted.md
- V1 to V2 migration: docs/PluginSDK2-Migration.md
- XAML development: docs/PluginSDK2-Xaml.md
- API reference: docs/PluginSDK2-API.md
- Package format: docs/PluginSDK2-Packages.md
- Validation and migration: docs/PluginSDK2-Testing.md
- Engineering notes: docs/PluginSDK2-EngineeringNotes.md

C# plugins reference the SDK and its WinRT projection at build time through the local NuGet feed in nuget/. Native AOT Publish produces the native plugin DLL; end users do not install the NuGet packages. C++ plugins consume the WinMD and headers in native/. PluginPack creates the .hutao package; PrototypeHost is an optional test host, not a hot-reload runtime.
The SDK nupkg contains Visual Studio project templates under tools/VisualStudio/. Copy those ZIPs to the Visual Studio user project templates directory, or copy a source template into your own working directory.
No production host or NuGet publication is implied by creating this directory.
"@ | Set-Content -LiteralPath (Join-Path $out 'README.md') -Encoding utf8
Get-ChildItem -LiteralPath $out -Recurse -File | Where-Object { $_.Extension -in '.nupkg', '.zip', '.exe', '.winmd' } |
    Get-FileHash -Algorithm SHA256 | Format-Table -AutoSize
Write-Host "Local SDK distribution: $out"
