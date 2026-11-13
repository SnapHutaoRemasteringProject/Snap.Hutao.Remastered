[CmdletBinding()]
param(
    [string]$ViewModelDirectory = (Join-Path $PSScriptRoot '../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel')
)

$ErrorActionPreference = 'Stop'
# Source-level guard for concrete page/slim VM declarations. A successful build
# must still verify generated accessors; this check is not runtime acceptance.
$pattern = 'public\s+(?:sealed\s+)?partial\s+class\s+\w+\s*:\s*(?:Abstraction\.)?ViewModel(?:Slim)?(?:<[^>]+>)?\b'
$missing = @(foreach ($file in Get-ChildItem -LiteralPath $ViewModelDirectory -Recurse -Filter '*.cs') {
    $source = [IO.File]::ReadAllText($file.FullName)
    if ($source -match $pattern -and $source -notmatch '\[(?:WinRT\.)?GeneratedBindableCustomProperty|\[BindableCustomPropertyProvider') {
        $file.FullName
    }
})
if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Output "Missing generated property-provider declaration: $_" }
    throw "$($missing.Count) concrete view models lack AOT binding providers."
}
Write-Output 'PASS: scanned concrete page/slim view models declare generated property providers.'
