param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$AppPackagesDirectory,
    [Parameter(Mandatory)][string]$StagingDirectory,
    [Parameter(Mandatory)][string]$CertificatePath,
    [Parameter(Mandatory)][string]$MakeAppxPath,
    [Parameter(Mandatory)][string]$SignToolPath,
    [ValidateSet('Publish', 'Alpha', 'Canary')][string]$Channel = 'Publish',
    [ValidateSet('R2R', 'NativeAOT')][string]$RuntimeKind = 'R2R',
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
    if ($Channel -eq 'Publish' -and $publisher -eq 'CN=469D671A-3AED-4488-AC4F-75E5394BC7FA') {
        throw 'Publish R2 cannot reuse the Microsoft Store publisher.'
    }
    $stage = [IO.Path]::GetFullPath($StagingDirectory)
    $output = Join-Path ([IO.Path]::GetFullPath($AppPackagesDirectory)) (Join-Path $Channel $RuntimeKind)
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
    if ($Channel -eq 'Publish') {
        # Repackage the Store release under the independently signed R2 publisher.
        $identity.SetAttribute('Publisher', $publisher)
    } elseif ($identity.GetAttribute('Publisher') -ne $publisher) {
        throw "The $Channel package publisher differs from the signing certificate."
    }
    $name = $identity.GetAttribute('Name')
    $version = $identity.GetAttribute('Version')
    $architecture = $identity.GetAttribute('ProcessorArchitecture')
    $expectedName = switch ($Channel) {
        'Publish' { 'SnapHutaoRemasteringProje.SnapHutaoRemastered' }
        'Alpha' { '7f0db578-026f-4e0b-a75b-d5d06bb0a74c' }
        'Canary' { '52127695-c6a7-406e-916a-693b905e8ba7' }
    }
    if ($name -ne $expectedName -or $architecture -ne 'x64' -or $version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "Unexpected release identity: $name $version $architecture"
    }
    $manifest.Save($manifestPath)

    $folderName = "Snap.Hutao.Remastered_$($version)_x64_$($Channel)_$($RuntimeKind)"
    $msixName = "$folderName.msix"
    $descriptorName = "Snap.Hutao.Remastered.$Channel.$RuntimeKind.x64.appinstaller"
    $folder = Join-Path $output $folderName
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $origin = "https://package.snaphutaorp.org/$Channel/$RuntimeKind/"
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
        $roots = @((Join-Path $source 'Dependencies\x64'), (Join-Path $sdkRoot 'Microsoft.VCLibs.140.00.UWPDesktop'))
        $candidates = @($roots | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object {
            Get-ChildItem -LiteralPath $_ -Recurse -File | Where-Object { $_.Extension -in @('.appx', '.msix') }
        })
        $dependenciesElement = $document.CreateElement('Dependencies', $namespace)
        foreach ($dependency in $dependencies) {
            $requiredName = $dependency.GetAttribute('Name')
            $minimum = [Version]$dependency.GetAttribute('MinVersion')
            $matches = @()
            foreach ($file in $candidates) {
                try {
                    $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
                    try {
                        $entry = $archive.GetEntry('AppxManifest.xml')
                        if ($null -eq $entry) { continue }
                        $reader = [IO.StreamReader]::new($entry.Open())
                        try { [xml]$frameworkManifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
                        $frameworkIdentity = $frameworkManifest.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Identity']")
                        if ($null -eq $frameworkIdentity) { continue }
                        if ($frameworkIdentity.GetAttribute('Name') -ne $requiredName) { continue }
                        if ($frameworkIdentity.GetAttribute('ProcessorArchitecture') -ne $architecture) { continue }
                        if ([Version]$frameworkIdentity.GetAttribute('Version') -lt $minimum) { continue }
                        $matches += [pscustomobject]@{
                            Path=$file.FullName
                            Name=$frameworkIdentity.GetAttribute('Name')
                            Publisher=$frameworkIdentity.GetAttribute('Publisher')
                            Version=[Version]$frameworkIdentity.GetAttribute('Version')
                        }
                    } finally { $archive.Dispose() }
                } catch [IO.InvalidDataException] {
                    continue
                }
            }
            $selected = $matches | Sort-Object Version -Descending | Select-Object -First 1
            if ($null -eq $selected) { throw "Missing x64 framework dependency: $requiredName (minimum $minimum)" }
            $destination = Join-Path $folder 'Dependencies\x64'
            New-Item -ItemType Directory -Path $destination -Force | Out-Null
            $filename = [IO.Path]::GetFileName($selected.Path)
            Copy-Item -LiteralPath $selected.Path -Destination (Join-Path $destination $filename) -Force
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
    ('<!doctype html><html lang="en"><meta charset="utf-8"><title>Snap Hutao Remastered ' + $Channel + ' ' + $RuntimeKind + '</title><a href="./' + $descriptorName + '">Download ' + $Channel + ' ' + $RuntimeKind + '</a></html>') |
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
