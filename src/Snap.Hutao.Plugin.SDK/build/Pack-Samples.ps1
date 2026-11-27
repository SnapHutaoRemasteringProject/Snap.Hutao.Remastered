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
    if ($item.Id -eq 'TestPlugin.Native') {
        # Explicit native component payload; never copy the host's dependency tree.
        $outputDir = Split-Path -Parent $item.Dll
        $candidates = @(Get-ChildItem -LiteralPath $outputDir -Filter 'PluginPage.xbf' -Recurse -File)
        if ($candidates.Count -eq 0) {
            $intermediate = Join-Path $root "samples\CppCompiled\obj\$Configuration"
            if (Test-Path -LiteralPath $intermediate) {
                $candidates = @(Get-ChildItem -LiteralPath $intermediate -Filter 'PluginPage.xbf' -Recurse -File)
            }
        }
        if ($candidates.Count -ne 1) { throw "Expected one PluginPage.xbf; found $($candidates.Count). Inspect the XAML compiler output." }
        $views = Join-Path $staging 'views'
        New-Item -ItemType Directory -Path $views -Force | Out-Null
        Copy-Item -LiteralPath $candidates[0].FullName -Destination (Join-Path $views 'PluginPage.xbf')
        # Retain the component metadata and PRI for diagnostics / later resource integration.
        foreach ($name in @('TestPlugin.Native.winmd', 'TestPlugin.Native.pri', 'resources.pri')) {
            $file = Join-Path $outputDir $name
            if (Test-Path -LiteralPath $file -PathType Leaf) { Copy-Item -LiteralPath $file -Destination $bin }
        }
    }
    if ($item.Id -ne 'TestPlugin.Native') {
        $views = Join-Path $staging 'views'
        New-Item -ItemType Directory -Path $views -Force | Out-Null
        $names = if ($item.Id -eq 'TestPlugin.Native.Core') { @('CoreView.xaml') } else { @('PluginPage.xaml', 'BindingPage.xaml') }
        foreach ($name in $names) {
            Copy-Item -LiteralPath (Join-Path $root ($item.Project + '\' + $name)) -Destination $views
        }
    }
    $template = Join-Path $root ($item.Project + '\' + $item.Template)
    $output = Join-Path $artifacts ($item.Id + '.hutao')
    & dotnet run --project $tool -c Release -- pack $template $staging $output
    if ($LASTEXITCODE -ne 0) { throw "Packaging failed: $($item.Id)" }
    Write-Host "Package: $output"
}
