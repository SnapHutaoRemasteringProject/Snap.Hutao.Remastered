[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][ValidateSet('R2R', 'NativeAOT')][string]$RuntimeKind
)

$ErrorActionPreference = 'Stop'
$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
if ($RuntimeKind -eq 'NativeAOT') {
    # Includes native PE, absence of CoreCLR/primary managed DLL, XAML and payload checks.
    & (Join-Path $PSScriptRoot 'Test-NativePublish.ps1') -PublishDirectory $publish
    return
}

foreach ($name in @('Snap.Hutao.Remastered.exe', 'Snap.Hutao.Remastered.dll', 'coreclr.dll',
        'hostfxr.dll', 'hostpolicy.dll', 'resources.pri')) {
    if ((Get-Item -LiteralPath (Join-Path $publish $name)).Length -eq 0) {
        throw "Empty R2R publish file: $name"
    }
}

# Inspect the primary assembly's ReadyToRun header, rather than mistaking the
# native apphost (which also has no CLR header) for the application runtime.
$stream = [IO.File]::OpenRead((Join-Path $publish 'Snap.Hutao.Remastered.dll'))
$reader = [IO.BinaryReader]::new($stream)
try {
    $stream.Position = 0x3c
    $pe = $reader.ReadUInt32()
    $stream.Position = $pe
    if ($reader.ReadUInt32() -ne 0x4550) { throw 'Invalid primary assembly PE signature' }
    $stream.Position = $pe + 6
    $sections = $reader.ReadUInt16()
    $stream.Position = $pe + 20
    $optionalSize = $reader.ReadUInt16()
    $optional = $pe + 24
    $stream.Position = $optional
    $magic = $reader.ReadUInt16()
    if ($magic -notin 0x10b, 0x20b) { throw 'Invalid primary assembly optional header' }
    $directories = if ($magic -eq 0x20b) { 112 } else { 96 }
    $stream.Position = $optional + $directories + 14 * 8
    $clrRva = $reader.ReadUInt32()
    if ($clrRva -eq 0 -or $reader.ReadUInt32() -lt 72) { throw 'Missing primary assembly CLR header' }
    $sectionTable = @(
        for ($index = 0; $index -lt $sections; $index++) {
            $stream.Position = $optional + $optionalSize + $index * 40 + 8
            $virtualSize = $reader.ReadUInt32()
            $virtualAddress = $reader.ReadUInt32()
            $rawSize = $reader.ReadUInt32()
            $rawOffset = $reader.ReadUInt32()
            [pscustomobject]@{ Start = $virtualAddress; Size = [Math]::Max($virtualSize, $rawSize); Raw = $rawOffset }
        }
    )
    function Resolve-Rva([uint32]$Address) {
        foreach ($section in $sectionTable) {
            if ($Address -ge $section.Start -and [long]$Address -lt [long]$section.Start + $section.Size) {
                return [long]$section.Raw + $Address - $section.Start
            }
        }
        throw "Unmapped PE address: $Address"
    }
    $stream.Position = (Resolve-Rva $clrRva) + 64
    $nativeRva = $reader.ReadUInt32()
    if ($nativeRva -eq 0 -or $reader.ReadUInt32() -eq 0) { throw 'Primary assembly is IL-only, not ReadyToRun' }
    $stream.Position = Resolve-Rva $nativeRva
    if ($reader.ReadUInt32() -ne 0x00525452) { throw 'Invalid ReadyToRun header signature' }
}
finally { $reader.Dispose(); $stream.Dispose() }
Write-Output 'PASS: R2R primary assembly and CoreCLR runtime present.'
