# Explicit sample staging: never package the entire Publish or Build output tree.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Debug','Release')][string] $Configuration,
      [ValidateSet('All','CSharpAot','CppCore','CppCompiled')][string] $Sample = 'All')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$tool = Join-Path $root 'tools\PluginPack\PluginPack.csproj'
foreach ($item in @(
    @{ Project = 'samples\CppCompiled'; Dll = (Join-Path $artifacts "native\$Configuration\compiled\TestPlugin.Native.dll"); Id = 'TestPlugin.Native'; Template = 'manifest.template.json' },
    @{ Project = 'samples\CppCore'; Dll = (Join-Path $artifacts "native\$Configuration\core\TestPlugin.Native.Core.dll"); Id = 'TestPlugin.Native.Core'; Template = 'manifest.template.json' },
    @{ Project = 'samples\CSharpAot'; Dll = (Join-Path $artifacts 'aot-plugin\TestPlugin.Aot.dll'); Id = 'TestPlugin'; Template = 'manifest.template.json' }
)) {
    if ($Sample -ne 'All' -and $item.Project -ne "samples\$Sample") { continue }
    if (!(Test-Path -LiteralPath $item.Dll -PathType Leaf)) { throw "Missing $($item.Dll). In Visual Studio build the selected C++ project ($Configuration|x64), or Publish CSharpAot using NativeAot. This script only packages existing outputs." }
    $staging = Join-Path $artifacts ("stage\" + $item.Id)
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    $bin = Join-Path $staging 'bin\x64'
    New-Item -ItemType Directory -Path $bin -Force | Out-Null
    Write-Host "Packing $($item.Id) from $($item.Dll)"
    Write-Host "Entry SHA256: $((Get-FileHash -LiteralPath $item.Dll -Algorithm SHA256).Hash)"
    Copy-Item -LiteralPath $item.Dll -Destination $bin
    if ($item.Id -in @('TestPlugin.Native', 'TestPlugin')) {
        # Only the component PRI is distributed; XBF must be embedded in it.
        $pri = [IO.Path]::ChangeExtension($item.Dll, '.pri')
        if ($item.Id -eq 'TestPlugin.Native' -and !(Test-Path -LiteralPath $pri -PathType Leaf)) {
            $pri = Join-Path (Split-Path -Parent $item.Dll) 'resources.pri'
        }
        if (!(Test-Path -LiteralPath $pri -PathType Leaf)) {
            throw "Compiled sample PRI missing: $pri. Build or publish the sample with embedded XBF enabled."
        }
        $packagePri = [IO.Path]::ChangeExtension([IO.Path]::GetFileName($item.Dll), '.pri')
        Copy-Item -LiteralPath $pri -Destination (Join-Path $bin $packagePri)
        Write-Host "Compiled XAML PRI: $pri (no standalone XAML or XBF is packaged)"
    }
    if ($item.Id -eq 'TestPlugin.Native.Core') {
        $views = Join-Path $staging 'views'
        New-Item -ItemType Directory -Path $views -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $root 'samples\CppCore\CoreView.xaml') -Destination $views
    }
    $template = Join-Path $root ($item.Project + '\' + $item.Template)
    $output = Join-Path $artifacts ($item.Id + '.hutao')
    & dotnet run --project $tool -c Release -- pack $template $staging $output
    if ($LASTEXITCODE -ne 0) { throw "Packaging failed: $($item.Id)" }
    Write-Host "Package: $output"
}
