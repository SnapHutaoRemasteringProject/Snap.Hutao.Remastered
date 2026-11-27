# Pack the source generator from an explicit local checkout into the app's local review feed.
# Does not checkout, modify, merge or publish the generator repository.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SourceGenerationRoot,
    [string] $FeedDirectory
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$source = (Resolve-Path -LiteralPath $SourceGenerationRoot).Path
$project = Join-Path $source 'src\Snap.Hutao.Remastered.SourceGeneration\Snap.Hutao.Remastered.SourceGeneration\Snap.Hutao.Remastered.SourceGeneration.csproj'
if (!(Test-Path -LiteralPath $project -PathType Leaf)) { throw "Source generator project not found: $project" }
if (!$FeedDirectory) { $FeedDirectory = Join-Path $repo 'artifacts\local-nuget' }
$feed = [IO.Path]::GetFullPath($FeedDirectory)
$commit = (& git -C $source rev-parse --short=12 HEAD)
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{12,}$') { throw 'Cannot identify the source generator checkout.' }
$dirty = & git -C $source status --porcelain
if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Use a clean source generator checkout so the package version identifies the exact source.' }
$version = "1.3.19-authoringreview.$commit"
New-Item -ItemType Directory -Force -Path $feed | Out-Null
$package = Join-Path $feed "Snap.Hutao.Remastered.SourceGeneration.$version.nupkg"
if (!(Test-Path -LiteralPath $package -PathType Leaf)) {
    & dotnet pack $project -c Release "-p:PackageVersion=$version" -o $feed
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $package -PathType Leaf)) { throw 'Source generator pack failed.' }
}
Write-Host "SourceGeneration commit: $commit"
Write-Host "Local NuGet feed: $feed"
Write-Host "Package version: $version"
Write-Host "Main project arguments: -p:HutaoSourceGenerationVersion=$version -p:HutaoSourceGenerationFeed=`"$feed`""
