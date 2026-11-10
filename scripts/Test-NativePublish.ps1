[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [switch]$Launch,
    [string]$ProfileRoot
)

$ErrorActionPreference = 'Stop'
$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
$executable = Join-Path $publish 'Snap.Hutao.Remastered.exe'
foreach ($relative in @('Snap.Hutao.Remastered.exe', 'resources.pri', 'App.xbf', 'Assets/Logo.ico',
        'Snap.Hutao.Remastered.Native.dll', 'Snap.HPatch.dll', 'YaeAchievementLib.dll', 'Snap.Hutao.Remastered.Deployment.exe')) {
    $file = Get-Item -LiteralPath (Join-Path $publish $relative)
    if ($file.Length -eq 0) { throw "Empty publish file: $relative" }
}
foreach ($folder in @('Assets', 'Resource')) {
    if (@(Get-ChildItem -LiteralPath (Join-Path $publish $folder) -File -Recurse).Count -eq 0) {
        throw "Empty publish folder: $folder"
    }
}
foreach ($runtime in @('coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll')) {
    if (Test-Path -LiteralPath (Join-Path $publish $runtime)) { throw "Unexpected CoreCLR runtime: $runtime" }
}
if (Test-Path -LiteralPath (Join-Path $publish 'Snap.Hutao.Remastered.dll')) {
    throw 'Publish output still contains the primary managed assembly; an apphost PE alone is not Native AOT'
}

$stream = [IO.File]::OpenRead($executable)
$reader = [IO.BinaryReader]::new($stream)
try {
    $stream.Position = 0x3c
    $peOffset = $reader.ReadUInt32()
    $stream.Position = $peOffset
    if ($reader.ReadUInt32() -ne 0x4550 -or $reader.ReadUInt16() -ne 0x8664) { throw 'Expected an x64 PE executable' }
    $stream.Position = $peOffset + 24
    if ($reader.ReadUInt16() -ne 0x20b) { throw 'Expected a PE32+ executable' }
    $stream.Position = $peOffset + 24 + 112 + 14 * 8
    if ($reader.ReadUInt32() -ne 0 -or $reader.ReadUInt32() -ne 0) { throw 'Executable still has a CLR header' }
}
finally { $reader.Dispose(); $stream.Dispose() }

Write-Output "PASS: x64 native PE without CLR header; application PRI/XBF, assets, resources and runtime payload present; no CoreCLR host."
if ($Launch) {
    if ([string]::IsNullOrWhiteSpace($ProfileRoot)) {
        $ProfileRoot = Join-Path ([IO.Path]::GetTempPath()) ('SnapHutaoAot-' + [Guid]::NewGuid().ToString('N'))
    }
    if (-not [IO.Path]::IsPathFullyQualified($ProfileRoot)) { throw 'ProfileRoot must be absolute' }
    $ProfileRoot = [IO.Path]::GetFullPath($ProfileRoot)
    New-Item -ItemType Directory -Path $ProfileRoot -Force | Out-Null
    $process = Start-Process -FilePath $executable -WorkingDirectory $publish -PassThru `
        -Environment @{ SNAP_HUTAO_PROFILE_ROOT = $ProfileRoot } `
        -RedirectStandardOutput (Join-Path $ProfileRoot 'stdout.log') `
        -RedirectStandardError (Join-Path $ProfileRoot 'stderr.log')
    [pscustomobject]@{ ProcessId = $process.Id; ProfileRoot = $ProfileRoot; Executable = $executable }
}
