param(
    [Parameter(Mandatory)][string]$InstallerPath,
    [string]$RclonePath = 'rclone'
)
$ErrorActionPreference = 'Stop'
$installer = Get-Item -LiteralPath $InstallerPath
if ($installer.PSIsContainer -or $installer.Name -notmatch '\ASnap\.Hutao\.Remastered-\d+\.\d+\.\d+\.\d+-Setup\.exe\z' -or $installer.Length -eq 0) {
    throw 'Only the nonempty default Snap.Hutao.Remastered-<version>-Setup.exe can be published to R2. Cache installers and archives are excluded.'
}
foreach ($key in @('RCLONE_CONFIG_R2_ACCESS_KEY_ID', 'RCLONE_CONFIG_R2_SECRET_ACCESS_KEY', 'RCLONE_CONFIG_R2_ENDPOINT')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) { throw "Missing R2 configuration: $key. Configure the R2_ACCESS_KEY_ID, R2_SECRET_ACCESS_KEY and R2_ENDPOINT Actions secrets." }
}
$endpoint = [Uri]$env:RCLONE_CONFIG_R2_ENDPOINT
if (!$endpoint.IsAbsoluteUri -or $endpoint.Scheme -ne 'https') { throw 'R2_ENDPOINT must be an HTTPS S3 API endpoint.' }
if ($endpoint.AbsolutePath.TrimEnd('/') -eq '/package') {
    # The dashboard may show the bucket URL; rclone expects the account endpoint separately.
    $env:RCLONE_CONFIG_R2_ENDPOINT = $endpoint.GetLeftPart([UriPartial]::Authority)
} elseif ($endpoint.AbsolutePath.Trim('/') -ne '') {
    throw 'R2_ENDPOINT must be the account S3 endpoint, optionally ending in /package, not the Worker management URL.'
}
if (!$endpoint.Host.EndsWith('.r2.cloudflarestorage.com', [StringComparison]::OrdinalIgnoreCase) -or $endpoint.UserInfo -or $endpoint.Query -or $endpoint.Fragment) {
    throw 'Use the Cloudflare R2 S3 API endpoint, not the Worker website or the public download domain.'
}
Get-Command $RclonePath -ErrorAction Stop | Out-Null
$remoteDirectory = 'r2:package/'
$destination = $remoteDirectory + $installer.Name
# Credentials come exclusively from environment variables, never command arguments or config files.
$config = if ($IsWindows) { 'NUL' } else { '/dev/null' }
& $RclonePath copyto $installer.FullName $destination --config $config --immutable `
    --s3-chunk-size 64Mi --s3-upload-concurrency 4 --retries 5 --low-level-retries 10 --stats 15s
if ($LASTEXITCODE -ne 0) { throw "R2 installer upload failed (rclone exit $LASTEXITCODE)." }
# Filter the check to the exact same basename; never include sibling cache EXEs or ZIPs.
& $RclonePath check $installer.Directory.FullName $remoteDirectory --config $config `
    --include "/$($installer.Name)" --one-way --size-only --retries 5 --low-level-retries 10
if ($LASTEXITCODE -ne 0) { throw "R2 installer size verification failed (rclone exit $LASTEXITCODE)." }
$url = "https://package.snaphutaorp.org/$($installer.Name)"
Write-Host "Published default installer: $url"
if ($env:GITHUB_STEP_SUMMARY) {
    "`n### R2 默认安装包`n`n[$($installer.Name)]($url)`n" | Out-File -LiteralPath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
if ($env:GITHUB_OUTPUT) { "download-url=$url" | Out-File -LiteralPath $env:GITHUB_OUTPUT -Append -Encoding utf8 }
