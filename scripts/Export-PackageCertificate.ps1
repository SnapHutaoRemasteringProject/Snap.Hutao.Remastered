param(
    [Parameter(Mandatory)][string]$PfxPath,
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:PW)) { throw 'PW signing secret is missing.' }
$pfx = (Resolve-Path -LiteralPath $PfxPath).Path
$certificate = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12FromFile(
    $pfx, $env:PW, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
try {
    $isCa = @($certificate.Extensions | Where-Object {
        $_ -is [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension] -and $_.CertificateAuthority
    }).Count -gt 0
    if ($isCa) { throw 'A CA certificate cannot be distributed as the sideload trust certificate.' }
    $hasSigningEku = @($certificate.Extensions | Where-Object {
        $_ -is [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension] -and
        @($_.EnhancedKeyUsages | Where-Object { $_.Value -eq '1.3.6.1.5.5.7.3.3' }).Count -gt 0
    }).Count -gt 0
    if (!$hasSigningEku) { throw 'Code-signing EKU is missing.' }
    [IO.File]::WriteAllBytes([IO.Path]::GetFullPath($OutputPath),
        $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    Write-Host "Exported sideload signing certificate: $OutputPath"
} finally {
    $certificate.Dispose()
}
