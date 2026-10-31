param(
    [string]$ArchiveUrl = $env:BUNDLED_IMAGES_URL,
    [string]$ResourceVersion = $env:BUNDLED_IMAGES_VERSION,
    [string]$Sha256 = $env:BUNDLED_IMAGES_SHA256
)
$ErrorActionPreference = 'Stop'
$ArchiveUrl = ([string]$ArchiveUrl).Trim()
$ResourceVersion = ([string]$ResourceVersion).Trim()
$Sha256 = ([string]$Sha256).Trim()
if (!$ArchiveUrl -and !$ResourceVersion -and !$Sha256) {
    Write-Host 'No bundled image snapshot configured; building without optional images.'
    return
}
if (!$ArchiveUrl -or !$ResourceVersion -or $Sha256 -notmatch '^[A-Fa-f0-9]{64}$') {
    throw 'Configure BUNDLED_IMAGES_URL, BUNDLED_IMAGES_VERSION and BUNDLED_IMAGES_SHA256 together.'
}
if ($ResourceVersion -notmatch '^\d+\.\d+(\.\d+)?$') { throw 'BUNDLED_IMAGES_VERSION must be a snapshot version such as 7.1.' }
$uri = [Uri]$ArchiveUrl
if (!$uri.IsAbsoluteUri -or $uri.Scheme -ne 'https') { throw 'The snapshot must use an HTTPS archive URL.' }
if ($uri.AbsolutePath.TrimEnd('/') -eq '/static/raw') {
    $resolved = [UriBuilder]::new($uri)
    $resolved.Path = $uri.AbsolutePath.TrimEnd('/') + '/' + $ResourceVersion + '.7z'
    $uri = $resolved.Uri
    Write-Warning "BUNDLED_IMAGES_URL points to a directory; resolved snapshot URL: $($uri.AbsoluteUri)"
}
$archiveExtension = [IO.Path]::GetExtension($uri.AbsolutePath).ToLowerInvariant()
if ($archiveExtension -notin @('.7z', '.zip')) { throw 'The snapshot URL must end in .7z or .zip.' }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$stage = Join-Path $repoRoot ('artifacts/image-snapshot-' + [Guid]::NewGuid().ToString('N'))
$archive = Join-Path $stage "snapshot$archiveExtension"
$extract = Join-Path $stage 'extracted'
$output = Join-Path $stage 'prepared'
[IO.Directory]::CreateDirectory($extract) | Out-Null
try {
    Invoke-WebRequest -Uri $uri -OutFile $archive
} catch {
    throw "Snapshot download failed. BUNDLED_IMAGES_URL must resolve to a downloadable .7z or .zip archive. $($_.Exception.Message)"
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $Sha256) { throw 'Snapshot SHA256 mismatch.' }
& 7z x $archive "-o$extract" -y | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Failed to extract bundled image snapshot.' }
$imageRoots = @(Get-ChildItem -LiteralPath $extract -Directory -Recurse | Where-Object { $_.Name -in @('raw', 'BundledImages') })
if ($imageRoots.Count -gt 1) { throw 'Ambiguous archive layout: multiple raw/BundledImages directories.' }
$imageRoot = if ($imageRoots.Count -eq 1) { $imageRoots[0].FullName } else { $extract }
& (Join-Path $PSScriptRoot 'Prepare-BundledImages.ps1') -SourceDirectory $imageRoot -ResourceVersion $ResourceVersion -OutputDirectory $output
# MSBuild imports environment variables as properties. Every Cake entry point consumes this.
$env:BundledImagesSource = $output
if ($env:GITHUB_ENV) { "BundledImagesSource=$output" | Out-File -LiteralPath $env:GITHUB_ENV -Append -Encoding utf8 }
