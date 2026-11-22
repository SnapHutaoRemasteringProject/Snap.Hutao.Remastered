# Regenerates the committed protobuf C# from the vendored schema.
#
# The application compiles `Proto\Generated\*.cs` directly; it does not run protoc during a normal
# build. Run this script only after updating the `.proto` files (see Proto\README.md).
#
#   powershell -File scripts/Generate-MetadataProto.ps1
#   powershell -File scripts/Generate-MetadataProto.ps1 -Check   # fails when the committed output is stale

[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$protoRoot = Join-Path $repoRoot 'src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Proto'
$generatedRoot = Join-Path $protoRoot 'Generated'
$grpcToolsVersion = '2.72.0'
$protoc = Join-Path $env:USERPROFILE ".nuget/packages/grpc.tools/$grpcToolsVersion/tools/windows_x64/protoc.exe"

if (-not (Test-Path $protoc))
{
    throw "protoc not found at '$protoc'. Restore the Grpc.Tools $grpcToolsVersion package first (dotnet restore)."
}

$protos = Get-ChildItem -Path $protoRoot -Filter '*.proto' | Sort-Object Name
if ($protos.Count -eq 0)
{
    throw "No .proto files found in '$protoRoot'."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("hutao-metadata-proto-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
try
{
    & $protoc "--csharp_out=$tempRoot" "--proto_path=$protoRoot" @($protos.FullName)
    if ($LASTEXITCODE -ne 0)
    {
        throw "protoc failed with exit code $LASTEXITCODE."
    }

    $generated = Get-ChildItem -Path $tempRoot -Filter '*.cs' | Sort-Object Name
    if ($generated.Count -ne $protos.Count)
    {
        throw "protoc generated $($generated.Count) files for $($protos.Count) schemas."
    }

    if ($Check)
    {
        $stale = @()
        foreach ($file in $generated)
        {
            $committed = Join-Path $generatedRoot $file.Name
            if (-not (Test-Path $committed) -or
                (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $committed).Hash)
            {
                $stale += $file.Name
            }
        }

        if ($stale.Count -gt 0)
        {
            throw "Proto/Generated is stale: $($stale -join ', '). Run scripts/Generate-MetadataProto.ps1 and commit the result."
        }

        Write-Host 'Proto/Generated matches the vendored schema.'
        return
    }

    if (Test-Path $generatedRoot)
    {
        Remove-Item -Path $generatedRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $generatedRoot | Out-Null
    foreach ($file in $generated)
    {
        Copy-Item -Path $file.FullName -Destination (Join-Path $generatedRoot $file.Name)
    }

    Write-Host "Regenerated $($generated.Count) files in Proto/Generated."
}
finally
{
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
