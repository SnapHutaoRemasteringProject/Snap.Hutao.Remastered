# Run from Visual Studio Developer PowerShell. C# always uses Native AOT Publish.
[CmdletBinding()]
param([ValidateSet('Debug','Release')][string] $Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
function Invoke-Checked([string] $Command, [string[]] $Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed: $LASTEXITCODE" }
}
Invoke-Checked dotnet @('build', (Join-Path $root 'libraries\Snap.Hutao.Plugin.Projection\Snap.Hutao.Plugin.Projection.csproj'), '-c', $Configuration, '-p:Platform=x64')
foreach ($project in @('samples\CppCore\TestPlugin.Native.Core.vcxproj', 'samples\CppCompiled\TestPlugin.Native.vcxproj')) {
    Invoke-Checked msbuild @((Join-Path $root $project), '/restore', '/t:Rebuild', "/p:Configuration=$Configuration", '/p:Platform=x64')
}
Invoke-Checked dotnet @('publish', (Join-Path $root 'samples\CSharpAot\TestPlugin.Aot.csproj'), '-c', 'Release', '-p:Platform=x64', '-p:PublishProfile=NativeAot')
& (Join-Path $PSScriptRoot 'Pack-Samples.ps1') -Configuration $Configuration
