param(
    [Parameter(Mandatory)][string]$AppPackagesPath,
    [Parameter(Mandatory)][ValidateSet('Publish', 'Alpha', 'Canary')][string]$Channel,
    [Parameter(Mandatory)][ValidateSet('R2R', 'NativeAOT')][string]$RuntimeKind,
    [ValidateRange(1, 720)][int]$PreviousVersionRetentionHours = 48,
    [string]$RclonePath = 'rclone'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Restrict all actions to one channel AND one runtime. Never purge r2:package/.
$root = (Resolve-Path -LiteralPath $AppPackagesPath).Path
$runtimeSegment = if ($RuntimeKind -eq 'NativeAOT') { 'AOT' } else { '' }
$channelPath = if ($runtimeSegment) { Join-Path $Channel $runtimeSegment } else { $Channel }
$runtimeSuffix = if ($runtimeSegment) { "_$runtimeSegment" } else { '' }
$descriptorSuffix = if ($runtimeSegment) { ".$runtimeSegment" } else { '' }
$directory = Join-Path $root $channelPath
$descriptorName = "Snap.Hutao.Remastered.$Channel$descriptorSuffix.x64.appinstaller"
$descriptorPath = Join-Path $directory $descriptorName
if (!(Test-Path -LiteralPath $descriptorPath -PathType Leaf)) { throw "Missing App Installer file: $descriptorPath" }

[xml]$descriptor = Get-Content -LiteralPath $descriptorPath -Raw
$main = $descriptor.DocumentElement.SelectSingleNode("*[local-name()='MainPackage']")
if ($null -eq $main) { throw 'App Installer MainPackage is missing.' }
$origin = "https://package.snaphutaorp.org/$Channel/"
if ($runtimeSegment) { $origin += "$runtimeSegment/" }
$expectedUri = "$origin$descriptorName"
if ($descriptor.DocumentElement.GetAttribute('Uri') -ne $expectedUri) { throw 'Unexpected App Installer update URL.' }
$msixUri = [Uri]$main.GetAttribute('Uri')
if ($msixUri.Scheme -ne 'https' -or !$msixUri.AbsoluteUri.StartsWith($origin, [StringComparison]::Ordinal)) {
    throw 'MSIX URI must be inside the selected channel/runtime.'
}
$relative = $msixUri.AbsoluteUri.Substring($origin.Length)
$pieces = $relative.Split('/')
$versionFolder = $pieces[0]
$folderPattern = '^Snap\.Hutao\.Remastered_\d+\.\d+\.\d+\.\d+_x64_' + $Channel + $runtimeSuffix + '$'
if ($pieces.Length -ne 2 -or $versionFolder -notmatch $folderPattern -or $pieces[1] -ne "$versionFolder.msix") {
    throw 'Unexpected versioned MSIX path.'
}
$folderPath = Join-Path $directory $versionFolder
$msixPath = Join-Path $folderPath $pieces[1]
if (!(Test-Path -LiteralPath $msixPath -PathType Leaf) -or (Get-Item -LiteralPath $msixPath).Length -eq 0) {
    throw 'MainPackage MSIX is missing or empty.'
}

foreach ($key in @('RCLONE_CONFIG_R2_ACCESS_KEY_ID', 'RCLONE_CONFIG_R2_SECRET_ACCESS_KEY', 'RCLONE_CONFIG_R2_ENDPOINT')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) { throw "Missing R2 setting: $key" }
}
$endpoint = [Uri]$env:RCLONE_CONFIG_R2_ENDPOINT
if (!$endpoint.IsAbsoluteUri -or $endpoint.Scheme -ne 'https') { throw 'R2_ENDPOINT must be HTTPS.' }
if ($endpoint.AbsolutePath.TrimEnd('/') -eq '/package') {
    $env:RCLONE_CONFIG_R2_ENDPOINT = $endpoint.GetLeftPart([UriPartial]::Authority)
} elseif ($endpoint.AbsolutePath.Trim('/') -ne '') {
    throw 'R2_ENDPOINT must be the account S3 endpoint, optionally ending in /package.'
}
if (!$endpoint.Host.EndsWith('.r2.cloudflarestorage.com', [StringComparison]::OrdinalIgnoreCase) -or $endpoint.UserInfo -or $endpoint.Query -or $endpoint.Fragment) {
    throw 'R2_ENDPOINT must be a Cloudflare R2 S3 account endpoint.'
}
Get-Command $RclonePath -ErrorAction Stop | Out-Null
$config = if ($IsWindows) { 'NUL' } else { '/dev/null' }
$remote = "r2:package/$Channel/"
if ($runtimeSegment) { $remote += "$runtimeSegment/" }
$remoteVersion = "$remote$versionFolder/"

# Prevent older queued CI jobs from rolling back a newer successfully
# published update descriptor. The workflow also serializes R2 publishing.
$existing = @(& $RclonePath lsf $remote --config $config --files-only)
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect existing R2 update descriptor.' }
if ($existing -contains $descriptorName) {
    $remoteXml = @(& $RclonePath cat ($remote + $descriptorName) --config $config)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read existing R2 update descriptor.' }
    [xml]$active = ($remoteXml -join [Environment]::NewLine)
    $activeMain = $active.DocumentElement.SelectSingleNode("*[local-name()='MainPackage']")
    if ($null -eq $activeMain) { throw 'Existing R2 update descriptor has no MainPackage.' }
    if ([Version]$activeMain.GetAttribute('Version') -gt [Version]$main.GetAttribute('Version')) {
        Write-Host 'Skipping stale workflow: a newer version is already published.'
        exit 0
    }
}

# Publish and verify all versioned objects BEFORE changing the stable pointer.
# The App Installer can still download the previous version during this stage.
$files = @(Get-ChildItem -LiteralPath $folderPath -File -Recurse)
if ($files.Count -eq 0) { throw 'The versioned package directory is empty.' }
foreach ($file in $files) {
    $relativeFile = [IO.Path]::GetRelativePath($folderPath, $file.FullName).Replace('\', '/')
    $mime = switch ($file.Extension.ToLowerInvariant()) {
        '.msix' { 'application/msix' }
        '.appx' { 'application/appx' }
        '.cer' { 'application/pkix-cert' }
        default { 'application/octet-stream' }
    }
    & $RclonePath copyto $file.FullName ($remoteVersion + $relativeFile) --config $config --immutable --metadata --metadata-set ("content-type=" + $mime) --metadata-set 'cache-control=public,max-age=31536000,immutable' --s3-chunk-size 64Mi --s3-upload-concurrency 4 --retries 5 --low-level-retries 10 --stats 15s
    if ($LASTEXITCODE -ne 0) { throw "Versioned object upload failed: $relativeFile" }
}
& $RclonePath check $folderPath $remoteVersion --config $config --one-way --size-only --retries 5 --low-level-retries 10
if ($LASTEXITCODE -ne 0) { throw 'Versioned object verification failed.' }

& $RclonePath copyto $descriptorPath ($remote + $descriptorName) --config $config --metadata --metadata-set 'content-type=application/appinstaller' --metadata-set 'cache-control=no-cache' --retries 5 --low-level-retries 10
if ($LASTEXITCODE -ne 0) { throw 'Stable App Installer descriptor upload failed.' }
& $RclonePath check $directory $remote --config $config --include ("/" + $descriptorName) --one-way --size-only
if ($LASTEXITCODE -ne 0) { throw 'Stable App Installer descriptor verification failed.' }

$index = Join-Path $directory 'index.html'
if (Test-Path -LiteralPath $index -PathType Leaf) {
    & $RclonePath copyto $index ($remote + 'index.html') --config $config --metadata --metadata-set 'content-type=text/html; charset=utf-8' --metadata-set 'cache-control=no-cache' --retries 5 --low-level-retries 10
    if ($LASTEXITCODE -ne 0) { throw 'Channel landing page upload failed.' }
}

# Reclaim storage only after the descriptor points to a verified package.
# A client may already have downloaded the prior descriptor and still be
# requesting its versioned MSIX. Retain recently published versions for a
# bounded grace period; otherwise publishing a new version breaks that client.
$cutoff = [DateTimeOffset]::UtcNow.AddHours(-$PreviousVersionRetentionHours)
$folders = @(& $RclonePath lsf $remote --config $config --dirs-only)
if ($LASTEXITCODE -ne 0) { throw 'Cannot list old package directories for cleanup.' }
foreach ($entry in $folders) {
    $oldFolder = $entry.Trim().TrimEnd('/')
    if ($oldFolder -notmatch $folderPattern -or $oldFolder -eq $versionFolder) {
        continue
    }

    $oldRemote = "$remote$oldFolder/"
    $metadataJson = @(& $RclonePath lsjson $oldRemote --config $config --files-only --recursive)
    if ($LASTEXITCODE -ne 0) { throw "Cannot inspect old package for cleanup: $oldFolder" }
    $objects = @($metadataJson -join [Environment]::NewLine | ConvertFrom-Json)
    if ($objects.Count -eq 0 -or @($objects | Where-Object { !$_.ModTime }).Count -gt 0) {
        # Never delete objects with unknown timestamps: cannot prove they are old.
        Write-Host "Keeping $oldFolder: missing timestamp metadata"
        continue
    }

    $lastModified = @($objects | ForEach-Object {
        [DateTimeOffset]::Parse($_.ModTime, [Globalization.CultureInfo]::InvariantCulture)
    } | Sort-Object -Descending | Select-Object -First 1)[0]
    if ($lastModified -gt $cutoff) {
        Write-Host "Keeping $oldFolder until its $PreviousVersionRetentionHours-hour grace period expires"
        continue
    }

    Write-Host "Pruning expired $Channel/$RuntimeKind version: $oldFolder"
    & $RclonePath purge $oldRemote --config $config --retries 5 --low-level-retries 10
    if ($LASTEXITCODE -ne 0) { throw "Unable to remove old version: $oldFolder" }
}
Write-Host "Published and pruned expired versions: $expectedUri"
if ($env:GITHUB_STEP_SUMMARY) {
    $summary = [Environment]::NewLine + '### R2 ' + $Channel + '/' + $RuntimeKind + [Environment]::NewLine + $expectedUri + [Environment]::NewLine
    $summary | Out-File -LiteralPath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
if ($env:GITHUB_OUTPUT) { "appinstaller-url=$expectedUri" | Out-File -LiteralPath $env:GITHUB_OUTPUT -Append -Encoding utf8 }
