[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$AssemblyPath,
    [string]$SourceDirectory = (Join-Path $PSScriptRoot '../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered')
)

$ErrorActionPreference = 'Stop'
# Verify that the CsWinRT annotations produced actual linker roots, rather than
# merely silencing the cast-site analyzer. This does not execute application code.
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

public static class WinRtCastMetadata
{
    public static string[] RootBlobs(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        var roots = new List<string>();
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            if (reader.GetString(type.Namespace) != "WinRT" || reader.GetString(type.Name) != "DynamicWindowsRuntimeCastInitializer") continue;
            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                if (reader.GetString(method.Name) != "InitializeDynamicWindowsRuntimeCast") continue;
                foreach (CustomAttributeHandle attributeHandle in method.GetCustomAttributes())
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
                    MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    if (constructor.Parent.Kind != HandleKind.TypeReference) continue;
                    TypeReference attributeType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                    if (reader.GetString(attributeType.Namespace) != "System.Diagnostics.CodeAnalysis" || reader.GetString(attributeType.Name) != "DynamicDependencyAttribute") continue;
                    // typeof(T) is serialized as the assembly-qualified type name in the
                    // attribute blob. The comma boundary prevents prefix type matches.
                    roots.Add(Encoding.UTF8.GetString(reader.GetBlobBytes(attribute.Value)));
                }
            }
        }
        return roots.ToArray();
    }
}
'@

$types = @(Get-ChildItem -LiteralPath $SourceDirectory -Recurse -Filter '*.cs' -File |
    Where-Object FullName -NotMatch '[\\/](obj|bin)[\\/]' |
    ForEach-Object {
        $source = [IO.File]::ReadAllText($_.FullName)
        foreach ($match in [regex]::Matches($source, '\[WinRT\.DynamicWindowsRuntimeCast\(typeof\(global::(?<type>[A-Za-z0-9_.]+)\)\)\]')) {
            $match.Groups['type'].Value
        }
    } | Sort-Object -Unique)
if ($types.Count -eq 0) { throw 'No source cast annotations were found.' }
$roots = [WinRtCastMetadata]::RootBlobs((Resolve-Path -LiteralPath $AssemblyPath).Path)
$missing = @($types | Where-Object {
    $type = $_
    -not ($roots | Where-Object { $_.Contains($type + ',') })
})
if ($missing.Count -gt 0) { throw "Missing emitted WinRT cast roots: $($missing -join ', ')" }
Write-Output "PASS: all $($types.Count) annotated WinRT cast types have emitted DynamicDependency roots."
