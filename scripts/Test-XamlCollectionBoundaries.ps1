[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$GeneratedDirectory
)

$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $GeneratedDirectory).Path
$files = @(Get-ChildItem -LiteralPath $directory -Recurse -Filter '*.g.cs' -File)
if ($files.Count -eq 0) { throw "No generated XAML C# files in $directory. Build the application first." }

# Check generated assignments, including custom dependency properties. x:Bind
# itself does not make a boxed immutable value a valid WinRT collection source.
$methodPattern = '(?ms)private void (?<method>Update_[A-Za-z0-9_]+)\((?<type>global::[^\r\n]*ImmutableArray[^\r\n]+) obj, int phase\)\s*\{(?<body>.*?)(?=\r?\n\s*private (?:void|bool|static|class)|\r?\n\s*public (?:void|bool|static|class))'
$setterPattern = 'XamlBindingSetters\.(?<setter>Set_\w+)\(this\.\w+, obj(?:\.Value)?, null\)'
$failures = @(foreach ($file in $files) {
    $text = [IO.File]::ReadAllText($file.FullName)
    foreach ($method in [regex]::Matches($text, $methodPattern)) {
        foreach ($setter in [regex]::Matches($method.Groups['body'].Value, $setterPattern)) {
            [pscustomobject]@{
                File = $file.FullName
                Method = $method.Groups['method'].Value
                Type = $method.Groups['type'].Value
                Setter = $setter.Groups['setter'].Value
            }
        }
    }
})

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Output "$($failure.File): $($failure.Method) passes $($failure.Type) directly to $($failure.Setter)"
    }
    throw "Found $($failures.Count) direct immutable collection assignments. Convert in managed code before the WinRT setter."
}

Write-Output "PASS: no direct ImmutableArray assignments to generated XAML setters in $($files.Count) files."
