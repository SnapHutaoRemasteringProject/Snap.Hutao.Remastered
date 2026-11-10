[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Native,
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'tools/Snap.Hutao.Database.Model/Snap.Hutao.Database.Model.csproj'
$hostDll = Join-Path $repository 'tools/Snap.Hutao.Database.Model/bin/Release/net11.0/Snap.Hutao.Database.Model.dll'

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed ($LASTEXITCODE): $args" }
}

# Bootstrap without interceptors so changed queries can always be regenerated.
Invoke-DotNet build $project -c Release '-p:GenerateEfQueries=true'
if ($Check) {
    Invoke-DotNet $hostDll --check-model $repository
    Invoke-DotNet $hostDll --check-fixtures $repository
    Invoke-DotNet $hostDll --check-queries $repository
}
else {
    Invoke-DotNet $hostDll --generate-model $repository
    # Query generation must see the new Native AOT accessors/model in the assembly.
    Invoke-DotNet build $project -c Release '-p:GenerateEfQueries=true'
    Invoke-DotNet $hostDll --generate-fixtures $repository
    Invoke-DotNet $hostDll --generate-queries $repository
}

# This ordinary build includes the official interceptors; never use the bootstrap
# property for acceptance, publishing, or the final application build.
Invoke-DotNet build $project -c Release
Invoke-DotNet $hostDll
if ($Native) {
    Invoke-DotNet publish $project -c Release -r $Runtime '-p:PublishAot=true' '-p:JsonSerializerIsReflectionEnabledByDefault=false'
    $executable = if ($Runtime.StartsWith('win-')) { 'Snap.Hutao.Database.Model.exe' } else { 'Snap.Hutao.Database.Model' }
    & (Join-Path $repository "tools/Snap.Hutao.Database.Model/bin/Release/net11.0/$Runtime/publish/$executable")
    if ($LASTEXITCODE -ne 0) { throw "Native database verification failed: $LASTEXITCODE" }
}
