# Run in an x64 Native Tools developer PowerShell.
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]] $WinAppSdkWinmd,
    [string] $WindowsSdkVersion = '10.0.28000.0',
    [string] $WindowsSdkRoot = (Join-Path ([Environment]::GetEnvironmentVariable('ProgramFiles(x86)')) 'Windows Kits\10')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = $PSScriptRoot
$out = Join-Path $root 'artifacts'
$metadata = Join-Path $out 'metadata'
$references = Join-Path $out 'references'
$headers = Join-Path $out 'headers'
$native = Join-Path $out 'native'

function Invoke-Checked([string] $Tool, [string[]] $Arguments) {
    & $Tool @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Tool failed: $LASTEXITCODE" }
}

if ($env:VSCMD_ARG_TGT_ARCH -notin @('x64', 'amd64')) { throw 'Use an x64 Native Tools developer shell.' }
$windowsWinmd = Join-Path $WindowsSdkRoot "UnionMetadata\$WindowsSdkVersion\Windows.winmd"
$sdkBin = Join-Path $WindowsSdkRoot "bin\$WindowsSdkVersion\x64"
$midl = Join-Path $sdkBin 'midl.exe'
$cppwinrt = Join-Path $sdkBin 'cppwinrt.exe'
$contract = Join-Path $metadata 'Snap.Hutao.Plugin.Contracts.winmd'
$idl = Join-Path $root 'Snap.Hutao.Plugin.Contracts\HutaoPlugin.idl'

foreach ($path in @($windowsWinmd, $midl, $cppwinrt, $idl)) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing $path" }
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
foreach ($dir in @($metadata, $references, $headers, $native)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}
foreach ($file in ($sourceFiles | Sort-Object Name)) {
    Write-Host "WinMD: $($file.FullName)"
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $references $file.Name)
}
$refs = @(Get-ChildItem -LiteralPath $references -Filter '*.winmd' -File | Sort-Object Name | ForEach-Object FullName)

$midlArgs = @('/nologo', '/winrt', '/nomidl', '/h', 'nul', '/metadata_dir', (Split-Path $windowsWinmd), '/reference', $windowsWinmd, '/winmd', $contract)
foreach ($ref in $refs) { $midlArgs += @('/reference', $ref) }
$midlArgs += $idl
Invoke-Checked $midl $midlArgs
if (!(Test-Path -LiteralPath $contract)) { throw 'MIDL did not produce the contract.' }
Invoke-Checked $cppwinrt (@('-input', $contract, $windowsWinmd) + $refs + @('-output', $headers))
Get-FileHash -Algorithm SHA256 -LiteralPath (@($contract, $windowsWinmd) + $refs) | Format-Table -AutoSize

Push-Location $native
try {
    Invoke-Checked 'cl.exe' @('/nologo', '/std:c++20', '/EHsc', '/MD', '/O2', '/LD', "/I$headers", "/I$(Join-Path $root 'Snap.Hutao.Plugin.Contracts')", (Join-Path $root 'TestPlugin.Native\TestPlugin.cpp'), '/link', '/MACHINE:X64', 'windowsapp.lib', '/OUT:TestPlugin.Native.dll')
} finally { Pop-Location }

Invoke-Checked 'dotnet' @('publish', (Join-Path $root 'TestPlugin.Aot\TestPlugin.Aot.csproj'), '-c', 'Release', '-r', 'win-x64', '-o', (Join-Path $out 'aot-plugin'))
foreach ($aot in @('false', 'true')) {
    $name = if ($aot -eq 'true') { 'aot-host' } else { 'jit-host' }
    Invoke-Checked 'dotnet' @('publish', (Join-Path $root 'tests\PrototypeHost\PrototypeHost.csproj'), '-c', 'Release', '-r', 'win-x64', "-p:PublishAot=$aot", '-p:PublishReadyToRun=false', '-o', (Join-Path $out $name))
}
Write-Host 'Build finished. Follow README.md to run all four combinations; build alone is not a G1 pass.'
