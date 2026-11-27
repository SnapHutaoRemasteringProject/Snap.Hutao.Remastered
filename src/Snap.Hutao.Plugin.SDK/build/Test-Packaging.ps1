# Negative fixtures use a locally built sample. No plugin is executed.
[CmdletBinding()]
param([string] $Package, [string] $ManagedDll)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$Package) { $Package = Join-Path $root 'artifacts\TestPlugin.hutao' }
$Package = (Resolve-Path -LiteralPath $Package).Path
$tool = Join-Path $root 'tools\PluginPack\PluginPack.csproj'
& dotnet build $tool -c Release -p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw 'PluginPack build failed.' }
function Check-Package([string] $Path, [bool] $ShouldPass) {
    & dotnet run --project $tool -c Release --no-build -p:Platform=x64 -- verify $Path
    if (($LASTEXITCODE -eq 0) -ne $ShouldPass) { throw "Unexpected validation result: $Path" }
}
Check-Package $Package $true
$temp = Join-Path ([IO.Path]::GetTempPath()) ('hutao-pack-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
function Fixture([string] $Name, [scriptblock] $Modify, [bool] $ShouldPass = $false) {
    $path = Join-Path $temp ($Name + '.hutao')
    Copy-Item -LiteralPath $Package -Destination $path
    $zip = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Update)
    try { & $Modify $zip }
    finally { $zip.Dispose() }
    Check-Package $path $ShouldPass
    Write-Host "PASS expected validation ($ShouldPass): $Name"
}
try {
    if ($ManagedDll) {
        $managed = (Resolve-Path -LiteralPath $ManagedDll).Path
        Fixture 'managed-entry-valid-hash' {
            param($z)
            $manifestEntry = $z.GetEntry('manifest.json')
            $reader = [IO.StreamReader]::new($manifestEntry.Open())
            try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json -AsHashtable }
            finally { $reader.Dispose() }
            $entryName = $manifest.runtime.entry
            $z.GetEntry($entryName).Delete()
            $entry = $z.CreateEntry($entryName)
            $bytes = [IO.File]::ReadAllBytes($managed)
            $stream = $entry.Open()
            try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
            ($manifest.files | Where-Object { $_.path -eq $entryName }).sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
            $manifestEntry.Delete()
            $manifestEntry = $z.CreateEntry('manifest.json')
            $writer = [IO.StreamWriter]::new($manifestEntry.Open())
            try { $writer.Write(($manifest | ConvertTo-Json -Depth 20 -Compress)) } finally { $writer.Dispose() }
        }
    }
    Fixture 'unlisted' { param($z) $z.CreateEntry('unexpected.txt') | Out-Null }
    Fixture 'traversal' { param($z) $z.CreateEntry('../outside.txt') | Out-Null }
    Fixture 'case-collision' { param($z) $z.CreateEntry('MANIFEST.JSON') | Out-Null }
    Fixture 'missing-manifest' { param($z) $z.GetEntry('manifest.json').Delete() }
    Fixture 'tampered-entry' {
        param($z)
        $entry = @($z.Entries | Where-Object { $_.FullName -like 'bin/x64/*.dll' })[0]
        $name = $entry.FullName
        $entry.Delete()
        $entry = $z.CreateEntry($name)
        $stream = $entry.Open()
        try { $stream.WriteByte(0) } finally { $stream.Dispose() }
    }
    function Edit-Manifest($z, [scriptblock] $edit) {
        $entry = $z.GetEntry('manifest.json')
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json -AsHashtable }
        finally { $reader.Dispose() }
        & $edit $manifest
        $entry.Delete()
        $entry = $z.CreateEntry('manifest.json')
        $writer = [IO.StreamWriter]::new($entry.Open())
        try { $writer.Write(($manifest | ConvertTo-Json -Depth 20 -Compress)) }
        finally { $writer.Dispose() }
    }
    $inspection = [IO.Compression.ZipFile]::OpenRead($Package)
    try {
        $reader = [IO.StreamReader]::new($inspection.GetEntry('manifest.json').Open())
        try { $packageManifest = $reader.ReadToEnd() | ConvertFrom-Json -AsHashtable }
        finally { $reader.Dispose() }
    } finally { $inspection.Dispose() }
    if ($packageManifest.ui.mode -eq 'compiled') {
        Fixture 'compiled-missing-pri' {
            param($z)
            $pri = [IO.Path]::ChangeExtension($packageManifest.runtime.entry, '.pri')
            $z.GetEntry($pri).Delete()
            Edit-Manifest $z { param($m) $m.files = @($m.files | Where-Object { $_.path -ne $pri }) }
        }
        Fixture 'compiled-standalone-xbf' {
            param($z)
            $name = 'views/Fixture.xbf'
            $bytes = [byte[]]@(1,2,3)
            $stream = $z.CreateEntry($name).Open()
            try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
            $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
            Edit-Manifest $z { param($m) $m.files += @{ path = $name; sha256 = $hash } }
        }
    }
    function Add-GameLibraryFixture($z, [byte[]] $bytes) {
        # The fixture is only inspected as PE data and is NEVER loaded into any process.
        $name = 'inject/Fixture.dll'
        $entry = $z.CreateEntry($name)
        $stream = $entry.Open()
        try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
        $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
        Edit-Manifest $z {
            param($m)
            $m.capabilities += 'game-libraries'
            $m.gameLibraries = @($name)
            $m.files += @{ path = $name; sha256 = $hash }
        }
    }
    Fixture 'game-library-valid-pe' {
        param($z)
        $entry = @($z.Entries | Where-Object { $_.FullName -like 'bin/x64/*.dll' })[0]
        $memory = [IO.MemoryStream]::new()
        $source = $entry.Open()
        try { $source.CopyTo($memory); $bytes = $memory.ToArray() }
        finally { $source.Dispose(); $memory.Dispose() }
        Add-GameLibraryFixture $z $bytes
    } $true
    Fixture 'game-library-invalid-pe-valid-hash' {
        param($z)
        Add-GameLibraryFixture $z ([byte[]]@(0, 1, 2, 3))
    }
    Fixture 'game-library-no-capability' {
        param($z)
        Edit-Manifest $z { param($m) $m.gameLibraries = @('inject/Example.dll') }
    }
    Fixture 'game-library-no-file' {
        param($z)
        Edit-Manifest $z { param($m) $m.capabilities += 'game-libraries'; $m.gameLibraries = @('inject/Missing.dll') }
    }
    Fixture 'game-library-wrong-path' {
        param($z)
        Edit-Manifest $z { param($m) $m.capabilities += 'game-libraries'; $m.gameLibraries = @('bin/x64/Example.dll') }
    }
    Fixture 'game-library-duplicate' {
        param($z)
        Edit-Manifest $z { param($m) $m.capabilities += 'game-libraries'; $m.gameLibraries = @('inject/Example.dll','inject/example.dll') }
    }
    Write-Host 'All selected package fixtures passed.'
}
finally { Remove-Item -LiteralPath $temp -Recurse -Force }
