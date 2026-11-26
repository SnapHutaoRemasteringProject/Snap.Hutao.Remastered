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

if ($env:VSCMD_ARG_TGT_ARCH -ne 'x64') { throw 'Use an x64 Native Tools developer shell.' }
$windowsWinmd = Join-Path $WindowsSdkRoot "UnionMetadata\$WindowsSdkVersion\Windows.winmd"
$sdkBin = Join-Path $WindowsSdkRoot "bin\$WindowsSdkVersion\x64"
$midl = Join-Path $sdkBin 'midl.exe'
$cppwinrt = Join-Path $sdkBin 'cppwinrt.exe'
$contract = Join-Path $metadata 'Snap.Hutao.Plugin.Contracts.winmd'
$idl = Join-Path $root 'Snap.Hutao.Plugin.Contracts\HutaoPlugin.idl'

foreach ($path in @($windowsWinmd, $midl, $cppwinrt, $idl)) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing $path" }
}

# Copy exact references into a single private directory for both generators.
if (Test-Path -LiteralPath $references) { Remove-Item -LiteralPath $references -Recurse -Force }
foreach ($dir in @($metadata, $references, $headers, $native)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}
foreach ($path in $WinAppSdkWinmd) {
    $resolved = (Resolve-Path -LiteralPath $path).Path
    if ([IO.Path]::GetExtension($resolved) -ne '.winmd') { throw "Not WinMD: $resolved" }
    $destination = Join-Path $references ([IO.Path]::GetFileName($resolved))
    if (Test-Path -LiteralPath $destination) { throw "Duplicate metadata: $destination" }
    Copy-Item -LiteralPath $resolved -Destination $destination
}
$refs = @(Get-ChildItem -LiteralPath $references -Filter '*.winmd' | Sort-Object Name | ForEach-Object FullName)
if (!(Test-Path (Join-Path $references 'Microsoft.UI.Xaml.winmd'))) { throw 'Microsoft.UI.Xaml.winmd is required.' }

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
