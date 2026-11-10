[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$assessment = Join-Path $repository 'bin/CsWinRT3Assessment'
New-Item -ItemType Directory -Path $assessment -Force | Out-Null
$overrides = Join-Path $assessment 'VersionOverride.targets'
@'
<Project>
  <ItemGroup>
    <PackageReference Update="Microsoft.Windows.CsWinRT" Version="3.0.0-preview.261104.1" />
  </ItemGroup>
</Project>
'@ | Set-Content -LiteralPath $overrides -Encoding utf8NoBOM

# Build the actual application with the complete production dependency graph.
# Separate restore/intermediate/output paths leave the current 2.x build intact.
& dotnet build (Join-Path $repository 'src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj') `
    -c Release '-p:Platform=x64' '-p:PublishReadyToRun=false' `
    '-p:TargetFramework=net11.0-windows10.0.26100.1' `
    '-p:TargetFrameworks=net11.0-windows10.0.26100.1' `
    '-p:WindowsSdkPackageVersion=10.0.26100.88-preview' `
    "-p:CustomAfterMicrosoftCommonTargets=$overrides" `
    "-p:MSBuildProjectExtensionsPath=$assessment/restore/" `
    "-p:IntermediateOutputPath=$assessment/obj/" `
    "-p:OutputPath=$assessment/output/"
if ($LASTEXITCODE -ne 0) { throw "CsWinRT 3 production application assessment failed: $LASTEXITCODE" }
