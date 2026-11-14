[CmdletBinding()]
param([Parameter(Mandatory)][string]$AssemblyPath)

$ErrorActionPreference = 'Stop'
# Inspect emitted IL, without loading the WinUI application or running its initializers.
# DisplayMemberPath remains a runtime binding even when ItemsSource uses x:Bind.
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

public static class ArchiveBindingContracts
{
    static readonly Dictionary<ushort, OpCode> Opcodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode))
        .Select(f => (OpCode)f.GetValue(null))
        .ToDictionary(o => unchecked((ushort)o.Value));

    static IEnumerable<string> StringLiterals(MetadataReader reader, byte[] il)
    {
        for (int i = 0; i < il.Length;)
        {
            ushort key = il[i++];
            if (key == 0xfe) key = (ushort)(0xfe00 | il[i++]);
            OpCode opcode = Opcodes[key];
            int size = opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                _ => 4
            };
            if (opcode.OperandType == OperandType.InlineString)
                yield return reader.GetUserString(MetadataTokens.UserStringHandle(BitConverter.ToInt32(il, i) & 0x00ffffff));
            i += size;
        }
    }

    public static string[] Missing(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        var expected = new Dictionary<string, string>
        {
            ["GachaArchive"] = "Uid",
            ["BackpackArchive"] = "Name",
            ["AchievementArchive"] = "Name"
        };
        var found = new HashSet<string>();
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            string name = reader.GetString(type.Name);
            if (reader.GetString(type.Namespace) != "Snap.Hutao.Remastered.Model.Entity" || !expected.TryGetValue(name, out string property)) continue;
            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                // Check the generated dispatch implementation, not just the source attribute.
                string methodName = reader.GetString(method.Name).Replace("global::", "");
                if (methodName != "Microsoft.UI.Xaml.Data.IBindableCustomPropertyImplementation.GetProperty" || method.RelativeVirtualAddress == 0) continue;
                byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().ToArray();
                if (StringLiterals(reader, il).Contains(property)) found.Add(name);
            }
        }
        return expected.Where(e => !found.Contains(e.Key)).Select(e => e.Key + "." + e.Value).ToArray();
    }
}
'@

$missing = [ArchiveBindingContracts]::Missing((Resolve-Path -LiteralPath $AssemblyPath).Path)
if ($missing.Count -gt 0) {
    throw "Missing emitted AOT property dispatch for DisplayMemberPath: $($missing -join ', '). Build current source before checking."
}
Write-Output 'PASS: GachaArchive.Uid, BackpackArchive.Name and AchievementArchive.Name have emitted AOT property dispatch.'
