# Negative fixtures use a locally built sample. No plugin is executed.
[CmdletBinding()]
param([string] $Package)
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
function Fixture([string] $Name, [scriptblock] $Modify) {
    $path = Join-Path $temp ($Name + '.hutao')
    Copy-Item -LiteralPath $Package -Destination $path
    $zip = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Update)
    try { & $Modify $zip }
    finally { $zip.Dispose() }
    Check-Package $path $false
    Write-Host "PASS rejection: $Name"
}
try {
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
    Write-Host 'All selected package fixtures passed.'
}
finally { Remove-Item -LiteralPath $temp -Recurse -Force }
