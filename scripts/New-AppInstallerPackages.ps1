param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$AppPackagesDirectory,
    [Parameter(Mandatory)][string]$StagingDirectory,
    [Parameter(Mandatory)][string]$CertificatePath,
    [Parameter(Mandatory)][string]$MakeAppxPath,
    [Parameter(Mandatory)][string]$SignToolPath,
    [ValidateSet('Publish', 'Alpha', 'Canary')][string]$Channel = 'Publish',
    [string]$PasswordEnvironmentVariable = 'PUBLISH_PW',
    [string]$ExportCertificatePath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$source = (Resolve-Path -LiteralPath $PublishDirectory).Path
$pfx = (Resolve-Path -LiteralPath $CertificatePath).Path
$password = [Environment]::GetEnvironmentVariable($PasswordEnvironmentVariable)
if ([string]::IsNullOrWhiteSpace($password)) { throw "Missing signing password environment variable: $PasswordEnvironmentVariable" }
if (!(Test-Path -LiteralPath $MakeAppxPath -PathType Leaf)) { throw "Missing makeappx: $MakeAppxPath" }
if (!(Test-Path -LiteralPath $SignToolPath -PathType Leaf)) { throw "Missing signtool: $SignToolPath" }

# Read identity from the actual framework APPX. File names and SDKManifest.xml
# alone are insufficient to establish the required version and publisher.
function Read-FrameworkPackageIdentity([IO.FileInfo]$File) {
    try {
        $archive = [IO.Compression.ZipFile]::OpenRead($File.FullName)
        try {
            $entry = $archive.GetEntry('AppxManifest.xml')
            if ($null -eq $entry) { return $null }
            $reader = [IO.StreamReader]::new($entry.Open())
            try { [xml]$frameworkManifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $identity = $frameworkManifest.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Identity']")
            if ($null -eq $identity) { return $null }
            return [pscustomobject]@{
                Path=$File.FullName
                Name=$identity.GetAttribute('Name')
                Architecture=$identity.GetAttribute('ProcessorArchitecture')
                Publisher=$identity.GetAttribute('Publisher')
                Version=[Version]$identity.GetAttribute('Version')
            }
        } finally { $archive.Dispose() }
    } catch [IO.InvalidDataException] {
        return $null
    }
}

$certificate = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12FromFile(
    $pfx, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
try {
    # Reuse the release certificate safety checks previously enforced by Inno Setup.
    $isCa = @($certificate.Extensions | Where-Object {
        $_ -is [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension] -and $_.CertificateAuthority
    }).Count -gt 0
    if ($isCa) { throw 'Refusing to distribute a CA certificate as the trust certificate.' }
    $hasCodeSigningEku = @($certificate.Extensions | Where-Object {
        $_ -is [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension] -and
        @($_.EnhancedKeyUsages | Where-Object { $_.Value -eq '1.3.6.1.5.5.7.3.3' }).Count -gt 0
    }).Count -gt 0
    if (!$hasCodeSigningEku) { throw 'Release certificate must have the code-signing EKU.' }
    $publisher = $certificate.Subject
    $stage = [IO.Path]::GetFullPath($StagingDirectory)
    $output = Join-Path ([IO.Path]::GetFullPath($AppPackagesDirectory)) $Channel
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
    New-Item -ItemType Directory -Path $stage, $output -Force | Out-Null
    Copy-Item -Path (Join-Path $source '*') -Destination $stage -Recurse -Force
    # Framework APPX files are external dependencies, not payload content.
    $stagedDependencies = Join-Path $stage 'Dependencies'
    if (Test-Path -LiteralPath $stagedDependencies) {
        Remove-Item -LiteralPath $stagedDependencies -Recurse -Force
    }

    $manifestPath = Join-Path $stage 'AppxManifest.xml'
    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    $identity = $manifest.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Identity']")
    if ($null -eq $identity) { throw 'No AppxManifest package identity.' }
    if ($identity.GetAttribute('Publisher') -ne $publisher) {
        throw "The $Channel package publisher differs from the signing certificate."
    }
    $name = $identity.GetAttribute('Name')
    $version = $identity.GetAttribute('Version')
    $architecture = $identity.GetAttribute('ProcessorArchitecture')
    $expectedName = switch ($Channel) {
        'Publish' { 'E8B6E2B3-D2A0-4435-A81D-2A16AAF405C8' }
        'Alpha' { '7f0db578-026f-4e0b-a75b-d5d06bb0a74c' }
        'Canary' { '52127695-c6a7-406e-916a-693b905e8ba7' }
    }
    if ($name -ne $expectedName -or $architecture -ne 'x64' -or $version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "Unexpected release identity: $name $version $architecture"
    }
    $manifest.Save($manifestPath)

    $folderName = "Snap.Hutao.Remastered_$($version)_x64_$Channel"
    $msixName = "$folderName.msix"
    $descriptorName = "Snap.Hutao.Remastered.$Channel.x64.appinstaller"
    $folder = Join-Path $output $folderName
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $origin = "https://package.snaphutaorp.org/$Channel/"
    $namespace = 'http://schemas.microsoft.com/appx/appinstaller/2021'
    $document = [xml]('<?xml version="1.0" encoding="utf-8"?><AppInstaller xmlns="' + $namespace + '"></AppInstaller>')
    $root = $document.DocumentElement
    $root.SetAttribute('Uri', "$origin$descriptorName")
    $root.SetAttribute('Version', $version)
    $main = $document.CreateElement('MainPackage', $namespace)
    foreach ($attr in @{ Name=$name; Version=$version; Publisher=$publisher; ProcessorArchitecture=$architecture; Uri="$origin$folderName/$msixName" }.GetEnumerator()) {
        $main.SetAttribute($attr.Key, [string]$attr.Value)
    }
    [void]$root.AppendChild($main)

    # Resolve actual framework package identity from the packaged APPX, not from
    # PackageDependency.MinVersion. Do not publish an incomplete dependency set.
    Add-Type -AssemblyName System.IO.Compression
    $dependencies = @($manifest.SelectNodes("//*[local-name()='PackageDependency']"))
    if ($dependencies.Count -gt 0) {
        $sdkRoot = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft SDKs\Windows Kits\10\ExtensionSDKs'
        # Visual Studio installs Desktop Bridge VCLibs in Microsoft.VCLibs.Desktop,
        # not under the framework's MSIX package identity (UWPDesktop).
        $roots = @(
            (Join-Path $source 'Dependencies\x64'),
            (Join-Path $sdkRoot 'Microsoft.VCLibs.Desktop'),
            (Join-Path $sdkRoot 'Microsoft.VCLibs')
        )
        $candidates = @($roots | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object {
            Get-ChildItem -LiteralPath $_ -Recurse -File | Where-Object { $_.Extension -in @('.appx', '.msix') }
        })
        $dependenciesElement = $document.CreateElement('Dependencies', $namespace)
        foreach ($dependency in $dependencies) {
            $requiredName = $dependency.GetAttribute('Name')
            $requiredPublisher = $dependency.GetAttribute('Publisher')
            $minimum = [Version]$dependency.GetAttribute('MinVersion')
            $matches = @()
            foreach ($file in $candidates) {
                $framework = Read-FrameworkPackageIdentity $file
                if ($null -eq $framework) { continue }
                if ($framework.Name -ne $requiredName -or $framework.Architecture -ne $architecture) { continue }
                if ($requiredPublisher -and $framework.Publisher -ne $requiredPublisher) { continue }
                if ($framework.Version -lt $minimum) { continue }
                $matches += $framework
            }

            $selected = $matches | Sort-Object Version -Descending | Select-Object -First 1
            if ($null -eq $selected -and $requiredName -eq 'Microsoft.VCLibs.140.00.UWPDesktop' -and $minimum -le [Version]'14.0.33728.0') {
                # GitHub runners do not guarantee the optional UWP C++ workload.
                # winget-pkgs pins a Microsoft-published 14.0.33728.0 x64 APPX
                # inside this specific winget-cli release archive. Verify the
                # official SHA256 before extracting, then inspect APPX identity.
                $dependencyTemp = Join-Path ([IO.Path]::GetTempPath()) ('hutao-vclibs-' + [guid]::NewGuid().ToString('N'))
                New-Item -ItemType Directory -Path $dependencyTemp -Force | Out-Null
                try {
                    $archivePath = Join-Path $dependencyTemp 'DesktopAppInstaller_Dependencies.zip'
                    $downloadUrl = 'https://github.com/microsoft/winget-cli/releases/download/v1.9.25180/DesktopAppInstaller_Dependencies.zip'
                    Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath
                    $expectedHash = 'EEBA62F08531C8669B3E5FB895CE8800AE66D798739CDA2B4AEF2A1D80A5F3C5'
                    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $expectedHash) {
                        throw 'Downloaded Microsoft VCLibs dependency archive failed SHA256 verification.'
                    }
                    $fallbackPath = Join-Path $dependencyTemp 'Microsoft.VCLibs.140.00.UWPDesktop_14.0.33728.0_x64.appx'
                    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
                    try {
                        $entry = $archive.GetEntry('x64/Microsoft.VCLibs.140.00.UWPDesktop_14.0.33728.0_x64.appx')
                        if ($null -eq $entry) { throw 'Microsoft VCLibs x64 APPX is missing from the verified archive.' }
                        $inputStream = $entry.Open()
                        try {
                            $outputStream = [IO.File]::Create($fallbackPath)
                            try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose() }
                        } finally { $inputStream.Dispose() }
                    } finally { $archive.Dispose() }

                    $framework = Read-FrameworkPackageIdentity (Get-Item -LiteralPath $fallbackPath)
                    if ($null -eq $framework -or $framework.Name -ne $requiredName -or
                        $framework.Architecture -ne $architecture -or $framework.Version -lt $minimum -or
                        ($requiredPublisher -and $framework.Publisher -ne $requiredPublisher)) {
                        throw 'Downloaded Microsoft VCLibs APPX does not satisfy the manifest dependency.'
                    }
                    $selected = $framework

                    # Copy immediately: the temporary download is removed below.
                    $destination = Join-Path $folder 'Dependencies\x64'
                    New-Item -ItemType Directory -Path $destination -Force | Out-Null
                    Copy-Item -LiteralPath $selected.Path -Destination (Join-Path $destination ([IO.Path]::GetFileName($selected.Path))) -Force
                    $selected.Path = Join-Path $destination ([IO.Path]::GetFileName($selected.Path))
                } finally {
                    Remove-Item -LiteralPath $dependencyTemp -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
            if ($null -eq $selected) { throw "Missing x64 framework dependency: $requiredName (minimum $minimum)" }
            $destination = Join-Path $folder 'Dependencies\x64'
            New-Item -ItemType Directory -Path $destination -Force | Out-Null
            $filename = [IO.Path]::GetFileName($selected.Path)
            $targetPath = Join-Path $destination $filename
            if ($selected.Path -ne $targetPath) {
                Copy-Item -LiteralPath $selected.Path -Destination $targetPath -Force
            }
            $element = $document.CreateElement('Package', $namespace)
            foreach ($attr in @{ Name=$selected.Name; Version=$selected.Version.ToString(); Publisher=$selected.Publisher; ProcessorArchitecture=$architecture; Uri="$origin$folderName/Dependencies/x64/$filename" }.GetEnumerator()) {
                $element.SetAttribute($attr.Key, [string]$attr.Value)
            }
            [void]$dependenciesElement.AppendChild($element)
        }
        [void]$root.AppendChild($dependenciesElement)
    }

    $settings = $document.CreateElement('UpdateSettings', $namespace)
    $onLaunch = $document.CreateElement('OnLaunch', $namespace)
    $onLaunch.SetAttribute('HoursBetweenUpdateChecks', '0')
    [void]$settings.AppendChild($onLaunch)
    [void]$root.AppendChild($settings)
    $descriptorPath = Join-Path $output $descriptorName
    $document.Save($descriptorPath)
    Copy-Item -LiteralPath $descriptorPath -Destination (Join-Path $stage $descriptorName)

    $msixPath = Join-Path $folder $msixName
    & $MakeAppxPath pack /d $stage /p $msixPath
    if ($LASTEXITCODE -ne 0) { throw "makeappx failed ($LASTEXITCODE)" }
    & $SignToolPath sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /f $pfx /p $password $msixPath
    if ($LASTEXITCODE -ne 0) { throw "MSIX signing failed ($LASTEXITCODE)" }

    $certBytes = $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
    [IO.File]::WriteAllBytes((Join-Path $folder 'Snap.Hutao.Remastered.cer'), $certBytes)
    if ($ExportCertificatePath) {
        [IO.File]::WriteAllBytes([IO.Path]::GetFullPath($ExportCertificatePath), $certBytes)
    }
    ('<!doctype html><html lang="en"><meta charset="utf-8"><title>Snap Hutao Remastered ' + $Channel + '</title><a href="./' + $descriptorName + '">Download ' + $Channel + '</a></html>') |
        Set-Content -LiteralPath (Join-Path $output 'index.html') -Encoding utf8

    # Validate that the signed package contains the same descriptor and publisher.
    $zip = [IO.Compression.ZipFile]::OpenRead($msixPath)
    try {
        $entry = $zip.GetEntry($descriptorName)
        if ($null -eq $entry) { throw 'Missing embedded App Installer descriptor.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $embedded = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($embedded -ne [IO.File]::ReadAllText($descriptorPath)) { throw 'Embedded descriptor does not match published descriptor.' }
        $reader = [IO.StreamReader]::new($zip.GetEntry('AppxManifest.xml').Open())
        try { [xml]$packedManifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $packedIdentity = $packedManifest.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Identity']")
        if ($packedIdentity.GetAttribute('Publisher') -ne $publisher -or $packedIdentity.GetAttribute('Name') -ne $name) {
            throw 'Signed MSIX identity mismatch.'
        }
    } finally { $zip.Dispose() }
    Write-Host "Generated and validated AppPackages: $output"
} finally {
    $certificate.Dispose()
}
