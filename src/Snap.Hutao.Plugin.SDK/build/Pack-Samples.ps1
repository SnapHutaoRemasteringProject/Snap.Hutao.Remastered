# Explicit sample staging: never package the entire Publish or Build output tree.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Debug','Release')][string] $Configuration)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$tool = Join-Path $root 'tools\PluginPack\PluginPack.csproj'
foreach ($sample in @(
    @{ Project = 'TestPlugin.Native'; Dll = (Join-Path $artifacts "native\$Configuration\compiled\TestPlugin.Native.dll"); Id = 'TestPlugin.Native'; Template = 'manifest.template.json' },
    @{ Project = 'TestPlugin.Native.Core'; Dll = (Join-Path $artifacts "native\$Configuration\core\TestPlugin.Native.Core.dll"); Id = 'TestPlugin.Native.Core'; Template = 'manifest.template.json' },
    @{ Project = 'TestPlugin.Aot'; Dll = (Join-Path $artifacts 'aot-plugin\TestPlugin.Aot.dll'); Id = 'TestPlugin'; Template = 'manifest.template.json' }
)) {
    if (!(Test-Path -LiteralPath $sample.Dll -PathType Leaf)) { throw "Build/publish first: $($sample.Dll)" }
    $staging = Join-Path $artifacts ("stage\" + $sample.Id)
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    $bin = Join-Path $staging 'bin\x64'
    New-Item -ItemType Directory -Path $bin -Force | Out-Null
    Write-Host "Packing $($sample.Id) from $($sample.Dll)"
    Copy-Item -LiteralPath $sample.Dll -Destination $bin
    if ($sample.Id -eq 'TestPlugin.Native') {
        # Explicit native component payload; never copy the host's dependency tree.
        $outputDir = Split-Path -Parent $sample.Dll
        $candidates = @(Get-ChildItem -LiteralPath $outputDir -Filter 'PluginPage.xbf' -Recurse -File)
        if ($candidates.Count -eq 0) {
            $intermediate = Join-Path $root "TestPlugin.Native\obj\$Configuration"
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
    if ($sample.Id -ne 'TestPlugin.Native') {
        $views = Join-Path $staging 'views'
        New-Item -ItemType Directory -Path $views -Force | Out-Null
        $names = if ($sample.Id -eq 'TestPlugin.Native.Core') { @('CoreView.hxaml') } else { @('PluginPage.hxaml', 'BindingPage.hxaml') }
        foreach ($name in $names) {
            Copy-Item -LiteralPath (Join-Path $root ($sample.Project + '\' + $name)) -Destination $views
        }
    }
    $template = Join-Path $root ($sample.Project + '\' + $sample.Template)
    $output = Join-Path $artifacts ($sample.Id + '.hutao')
    & dotnet run --project $tool -c Release -- pack $template $staging $output
    if ($LASTEXITCODE -ne 0) { throw "Packaging failed: $($sample.Id)" }
}
