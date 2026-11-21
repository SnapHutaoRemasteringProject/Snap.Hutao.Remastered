param(
    [Parameter(Mandatory)][string]$AppPackagesPath,
    [Parameter(Mandatory)][ValidateSet('Publish', 'Alpha', 'Canary')][string]$Channel,
    [string]$RclonePath = 'rclone'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Restrict all actions to one channel. Never purge r2:package/.
$root = (Resolve-Path -LiteralPath $AppPackagesPath).Path
$directory = Join-Path $root $Channel
$descriptorName = "Snap.Hutao.Remastered.$Channel.x64.appinstaller"
$descriptorPath = Join-Path $directory $descriptorName
if (!(Test-Path -LiteralPath $descriptorPath -PathType Leaf)) { throw "Missing App Installer file: $descriptorPath" }

[xml]$descriptor = Get-Content -LiteralPath $descriptorPath -Raw
$main = $descriptor.DocumentElement.SelectSingleNode("*[local-name()='MainPackage']")
if ($null -eq $main) { throw 'App Installer MainPackage is missing.' }
$origin = "https://package.snaphutaorp.org/$Channel/"
$expectedUri = "$origin$descriptorName"
if ($descriptor.DocumentElement.GetAttribute('Uri') -ne $expectedUri) { throw 'Unexpected App Installer update URL.' }
$msixUri = [Uri]$main.GetAttribute('Uri')
if ($msixUri.Scheme -ne 'https' -or !$msixUri.AbsoluteUri.StartsWith($origin, [StringComparison]::Ordinal)) {
    throw 'MSIX URI must be inside the selected channel.'
}
$relative = $msixUri.AbsoluteUri.Substring($origin.Length)
$pieces = $relative.Split('/')
$versionFolder = $pieces[0]
$folderPattern = '^Snap\.Hutao\.Remastered_\d+\.\d+\.\d+\.\d+_x64_' + $Channel + '$'
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

# Ensure the new version is present before clearing the currently published feed.
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

# Retire old versions only after their replacement and descriptor are verified.
$remoteDirectories = @(& $RclonePath lsf $remote --config $config --dirs-only)
if ($LASTEXITCODE -ne 0) { throw 'Unable to list previous release folders.' }
foreach ($entry in $remoteDirectories) {
    $oldVersion = $entry.TrimEnd('/')
    if ($oldVersion -ne $versionFolder -and $oldVersion -match $folderPattern) {
        & $RclonePath purge ($remote + $oldVersion + '/') --config $config --retries 5 --low-level-retries 10
        if ($LASTEXITCODE -ne 0) { throw "Unable to remove previous release folder: $oldVersion" }
    }
}

Write-Host "Published $Channel feed: $expectedUri"
if ($env:GITHUB_STEP_SUMMARY) {
    $summary = [Environment]::NewLine + '### R2 ' + $Channel + [Environment]::NewLine + $expectedUri + [Environment]::NewLine
    $summary | Out-File -LiteralPath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
if ($env:GITHUB_OUTPUT) { "appinstaller-url=$expectedUri" | Out-File -LiteralPath $env:GITHUB_OUTPUT -Append -Encoding utf8 }
