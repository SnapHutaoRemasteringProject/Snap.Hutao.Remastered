param(
    [Parameter(Mandatory)][string]$AppPackagesPath,
    [string]$RclonePath = 'rclone'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$directory = (Resolve-Path -LiteralPath $AppPackagesPath).Path
$descriptorPath = Join-Path $directory 'Snap.Hutao.Remastered_x64.appinstaller'
if (!(Test-Path -LiteralPath $descriptorPath -PathType Leaf)) { throw 'App Installer file is missing.' }
[xml]$descriptor = Get-Content -LiteralPath $descriptorPath -Raw
$main = $descriptor.DocumentElement.SelectSingleNode("*[local-name()='MainPackage']")
if ($null -eq $main) { throw 'MainPackage is missing.' }
$expectedUri = 'https://package.snaphutaorp.org/Snap.Hutao.Remastered_x64.appinstaller'
if ($descriptor.DocumentElement.GetAttribute('Uri') -ne $expectedUri) { throw 'Unexpected descriptor update URL.' }
$msixUri = [Uri]$main.GetAttribute('Uri')
if ($msixUri.Scheme -ne 'https' -or $msixUri.Host -ne 'package.snaphutaorp.org') { throw 'Unexpected MSIX download host.' }
$relative = $msixUri.AbsolutePath.TrimStart('/')
$folderName = $relative.Split('/')[0]
if ($folderName -notmatch '^Snap\.Hutao\.Remastered_\d+\.\d+\.\d+\.\d+_x64_Release$') { throw 'Unexpected version folder.' }
$folderPath = Join-Path $directory $folderName
$msixPath = Join-Path $directory ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
if (!(Test-Path -LiteralPath $msixPath -PathType Leaf) -or (Get-Item -LiteralPath $msixPath).Length -eq 0) { throw 'MainPackage MSIX is missing or empty.' }

foreach ($key in @('RCLONE_CONFIG_R2_ACCESS_KEY_ID', 'RCLONE_CONFIG_R2_SECRET_ACCESS_KEY', 'RCLONE_CONFIG_R2_ENDPOINT')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) { throw "Missing R2 setting: $key" }
}
$endpoint = [Uri]$env:RCLONE_CONFIG_R2_ENDPOINT
if (!$endpoint.IsAbsoluteUri -or $endpoint.Scheme -ne 'https') { throw 'R2_ENDPOINT must be HTTPS.' }
if ($endpoint.AbsolutePath.TrimEnd('/') -eq '/package') {
    $env:RCLONE_CONFIG_R2_ENDPOINT = $endpoint.GetLeftPart([UriPartial]::Authority)
} elseif ($endpoint.AbsolutePath.Trim('/') -ne '') {
    throw 'R2_ENDPOINT must be the S3 endpoint, optionally ending with /package.'
}
if (!$endpoint.Host.EndsWith('.r2.cloudflarestorage.com', [StringComparison]::OrdinalIgnoreCase) -or $endpoint.UserInfo -or $endpoint.Query -or $endpoint.Fragment) {
    throw 'R2_ENDPOINT must be a Cloudflare R2 account endpoint.'
}
Get-Command $RclonePath -ErrorAction Stop | Out-Null
$config = if ($IsWindows) { 'NUL' } else { '/dev/null' }
$remote = 'r2:package/'
$remoteVersion = "$remote$folderName/"

# Immutable versioned data first. The stable pointer is uploaded last.
# Never sync-delete old releases: older App Installer clients may need them.
$packages = @((Get-Item -LiteralPath $msixPath))
$dependencyDirectory = Join-Path $folderPath 'Dependencies'
if (Test-Path -LiteralPath $dependencyDirectory) {
    $packages += @(Get-ChildItem -LiteralPath $dependencyDirectory -Recurse -File | Where-Object { $_.Extension -in @('.appx', '.msix') })
}
foreach ($package in $packages) {
    $relativePackage = [IO.Path]::GetRelativePath($folderPath, $package.FullName).Replace('\', '/')
    $mime = if ($package.Extension -eq '.appx') { 'application/appx' } else { 'application/msix' }
    & $RclonePath copyto $package.FullName ($remoteVersion + $relativePackage) --config $config --immutable --metadata --metadata-set ("content-type=" + $mime) --metadata-set 'cache-control=public,max-age=31536000,immutable' --s3-chunk-size 64Mi --s3-upload-concurrency 4 --retries 5 --low-level-retries 10 --stats 15s
    if ($LASTEXITCODE -ne 0) { throw "Versioned package upload failed ($LASTEXITCODE): $relativePackage" }
}
& $RclonePath copy $folderPath $remoteVersion --config $config --immutable --s3-chunk-size 64Mi --s3-upload-concurrency 4 --retries 5 --low-level-retries 10 --stats 15s
if ($LASTEXITCODE -ne 0) { throw "MSIX/dependencies upload failed ($LASTEXITCODE)" }
& $RclonePath check $folderPath $remoteVersion --config $config --one-way --size-only --retries 5 --low-level-retries 10
if ($LASTEXITCODE -ne 0) { throw "MSIX/dependencies check failed ($LASTEXITCODE)" }
& $RclonePath copyto $descriptorPath ($remote + 'Snap.Hutao.Remastered_x64.appinstaller') --config $config --metadata --metadata-set 'content-type=application/appinstaller' --metadata-set 'cache-control=no-cache' --retries 5 --low-level-retries 10
if ($LASTEXITCODE -ne 0) { throw "App Installer publish failed ($LASTEXITCODE)" }
& $RclonePath check $directory $remote --config $config --include '/Snap.Hutao.Remastered_x64.appinstaller' --one-way --size-only
if ($LASTEXITCODE -ne 0) { throw "App Installer check failed ($LASTEXITCODE)" }
$index = Join-Path $directory 'index.html'
if (Test-Path -LiteralPath $index -PathType Leaf) {
    & $RclonePath copyto $index ($remote + 'index.html') --config $config --metadata --metadata-set 'content-type=text/html; charset=utf-8' --metadata-set 'cache-control=no-cache' --retries 5 --low-level-retries 10
    if ($LASTEXITCODE -ne 0) { throw "Index upload failed ($LASTEXITCODE)" }
}
Write-Host "Published: $expectedUri"
if ($env:GITHUB_STEP_SUMMARY) {
    $summary = [Environment]::NewLine + '### R2 App Installer' + [Environment]::NewLine + $expectedUri + [Environment]::NewLine
    $summary | Out-File -LiteralPath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
if ($env:GITHUB_OUTPUT) { "appinstaller-url=$expectedUri" | Out-File -LiteralPath $env:GITHUB_OUTPUT -Append -Encoding utf8 }
