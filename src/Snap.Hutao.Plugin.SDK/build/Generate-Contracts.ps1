# Internal MSBuild target helper. Do not invoke for ordinary plugin development.
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $AssetsFile,
    [string[]] $WinAppSdkWinmd = @(),
    [version] $TargetPlatformVersion = '10.0.26100.0',
    [string] $VisualStudioInstallDir,
    [string] $WindowsSdkVersion = '10.0.28000.0',
    [string] $WindowsSdkRoot = (Join-Path ([Environment]::GetEnvironmentVariable('ProgramFiles(x86)')) 'Windows Kits\10')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'artifacts'
$metadata = Join-Path $out 'metadata'
$references = Join-Path $out 'references'

function Invoke-Checked([string] $Tool, [string[]] $Arguments) {
    & $Tool @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Tool failed: $LASTEXITCODE" }
}

# A C# MSBuild Exec task does not inherit the VC compiler's PATH.
# Prefer the VS instance that launched MSBuild, then the active developer
# environment, then vswhere. Do not hardcode an edition or a compiler minor version.
function Find-MsvcCompiler {
    $install = $VisualStudioInstallDir
    if (!$install) { $install = $env:VSINSTALLDIR }
    if (!$install -and $env:VCToolsInstallDir) {
        $candidate = Join-Path $env:VCToolsInstallDir 'bin\Hostx64\x64\cl.exe'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    if (!$install) {
        $vswhere = Join-Path ([Environment]::GetEnvironmentVariable('ProgramFiles(x86)')) 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
            $install = & $vswhere -latest -prerelease -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath | Select-Object -First 1
        }
    }
    if ($install) {
        $versionFile = Join-Path $install 'VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt'
        if (Test-Path -LiteralPath $versionFile -PathType Leaf) {
            $version = (Get-Content -LiteralPath $versionFile -Raw).Trim()
            $candidate = Join-Path $install "VC\Tools\MSVC\$version\bin\Hostx64\x64\cl.exe"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
        }
    }
    $command = Get-Command cl.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) { return $command.Source }
    throw 'MSVC x64 cl.exe not found. Install the Desktop development with C++ workload and its x64/x86 build tools.'
}

$compiler = Find-MsvcCompiler
Write-Host "MIDL preprocessor: $compiler"
$env:PATH = (Split-Path -Parent $compiler) + ';' + $env:PATH

$windowsWinmd = Join-Path $WindowsSdkRoot "UnionMetadata\$WindowsSdkVersion\Windows.winmd"
$sdkBin = Join-Path $WindowsSdkRoot "bin\$WindowsSdkVersion\x64"
$midl = Join-Path $sdkBin 'midl.exe'
$contract = Join-Path $metadata 'Snap.Hutao.Plugin.Contracts.winmd'
$idl = Join-Path $root 'contracts\HutaoPlugin.idl'

foreach ($path in @($windowsWinmd, $midl, $idl)) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing $path" }
}

# In MSBuild, derive the metadata closure from this project's actual NuGet restore.
# Do not enumerate all installed versions under the global package cache.
if ($AssetsFile) {
    $assets = Get-Content -LiteralPath $AssetsFile -Raw | ConvertFrom-Json
    $packageFolders = @($assets.packageFolders.PSObject.Properties | ForEach-Object Name)
    $resolvedMetadata = @(
        foreach ($library in $assets.libraries.PSObject.Properties) {
            if ($library.Value.type -ne 'package') { continue }
            $selected = @{}
            if ($library.Name -match '^Microsoft\.WindowsAppSDK(?:\.[^/]+)?/') {
                # Prefer versioned metadata compatible with the target platform.
                # Unversioned metadata is a fallback, not a second input.
                foreach ($relative in $library.Value.files) {
                    if ($relative -match '^metadata/[^/]+\.winmd$') {
                        $selected[[IO.Path]::GetFileName($relative)] = @{ Path = $relative; Version = [version]'0.0' }
                    }
                }
                foreach ($relative in $library.Value.files) {
                    if ($relative -match '^metadata/(\d+\.\d+\.\d+\.\d+)/([^/]+\.winmd)$') {
                        $version = [version]$Matches[1]
                        $name = $Matches[2]
                        if ($version -gt $TargetPlatformVersion) { continue }
                        if (!$selected.ContainsKey($name) -or $version -gt $selected[$name].Version) {
                            $selected[$name] = @{ Path = $relative; Version = $version }
                        }
                    }
                }
            } elseif ($library.Name -match '^Microsoft\.Web\.WebView2/') {
                foreach ($relative in $library.Value.files) {
                    if ($relative -eq 'lib/Microsoft.Web.WebView2.Core.winmd') {
                        $selected['Microsoft.Web.WebView2.Core.winmd'] = @{ Path = $relative }
                    }
                }
            }
            foreach ($entry in ($selected.Values | Sort-Object Path)) {
                $relative = $entry.Path
                $found = $null
                foreach ($folder in $packageFolders) {
                    $candidate = Join-Path (Join-Path $folder $library.Value.path) $relative
                    if (Test-Path -LiteralPath $candidate -PathType Leaf) { $found = $candidate; break }
                }
                if (!$found) { throw "Restored WinMD missing: $($library.Name)/$relative" }
                $found
            }
        }
    )
    if ($resolvedMetadata.Count -eq 0) { throw "No Windows App SDK metadata found in $AssetsFile. Restore the projection project first." }
    $WinAppSdkWinmd += $resolvedMetadata
}

# Accept explicit WinMD files or metadata directories. Validate all inputs before
# changing the private reference directory; never choose a different package version.
$sourceFiles = @(
    foreach ($path in $WinAppSdkWinmd) {
        $item = Get-Item -LiteralPath $path
        if ($item.PSIsContainer) {
            $matches = @(Get-ChildItem -LiteralPath $item.FullName -Filter '*.winmd' -File | Sort-Object Name)
            if ($matches.Count -eq 0) { throw "No WinMD files in metadata directory: $($item.FullName)" }
            $matches
        } elseif ($item.Extension -eq '.winmd') {
            $item
        } else {
            throw "Not a WinMD file or metadata directory: $($item.FullName)"
        }
    }
)
$byName = @{}
foreach ($file in $sourceFiles) {
    if ($byName.ContainsKey($file.Name)) {
        throw "Duplicate metadata name '$($file.Name)': $($byName[$file.Name].FullName) and $($file.FullName)"
    }
    if ($file.FullName.StartsWith($references.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Input metadata cannot be inside the generated references directory: $($file.FullName)"
    }
    $byName.Add($file.Name, $file)
}
if (!$byName.ContainsKey('Microsoft.UI.Xaml.winmd')) { throw 'Microsoft.UI.Xaml.winmd is required.' }

# Copy exact references into a single private directory for both generators.
if (Test-Path -LiteralPath $references) { Remove-Item -LiteralPath $references -Recurse -Force }
foreach ($dir in @($metadata, $references)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}
foreach ($file in ($sourceFiles | Sort-Object Name)) {
    Write-Host "WinMD: $($file.FullName)"
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $references $file.Name)
}
$refs = @(Get-ChildItem -LiteralPath $references -Filter '*.winmd' -File | Sort-Object Name | ForEach-Object FullName)

$includeRoot = Join-Path $WindowsSdkRoot "Include\$WindowsSdkVersion"
$midlArgs = @('/nologo', '/winrt', '/nomidl', '/h', 'nul', '/I', (Join-Path $includeRoot 'winrt'), '/I', (Join-Path $includeRoot 'shared'), '/I', (Join-Path $includeRoot 'um'), '/metadata_dir', (Split-Path $windowsWinmd), '/reference', $windowsWinmd, '/winmd', $contract)
if (Test-Path -LiteralPath $contract) { Remove-Item -LiteralPath $contract -Force }
foreach ($ref in $refs) { $midlArgs += @('/reference', $ref) }
$midlArgs += $idl
Invoke-Checked $midl $midlArgs
if (!(Test-Path -LiteralPath $contract)) { throw 'MIDL did not produce the contract.' }
Write-Host "Generated contract: $contract"

