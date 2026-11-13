[CmdletBinding()]
param([Parameter(Mandatory)][string]$AssemblyPath)
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

public sealed class HttpJsonContractInspection : ISignatureTypeProvider<string, object>
{
    public string GetArrayType(string t, ArrayShape s) => t + "[" + new string(',', s.Rank - 1) + "]";
    public string GetByReferenceType(string t) => t;
    public string GetFunctionPointerType(MethodSignature<string> s) => "!function";
    public string GetGenericInstantiation(string t, ImmutableArray<string> a) => Regex.Replace(t, "`[0-9]+", "") + "<" + string.Join(", ", a) + ">";
    public string GetGenericMethodParameter(object c, int n) => "!!" + n;
    public string GetGenericTypeParameter(object c, int n) => "!" + n;
    public string GetModifiedType(string m, string t, bool r) => t;
    public string GetPinnedType(string t) => t;
    public string GetPointerType(string t) => t + "*";
    public string GetPrimitiveType(PrimitiveTypeCode t) => t switch {
        PrimitiveTypeCode.Boolean => "bool", PrimitiveTypeCode.Byte => "byte", PrimitiveTypeCode.SByte => "sbyte",
        PrimitiveTypeCode.Char => "char", PrimitiveTypeCode.Int16 => "short", PrimitiveTypeCode.UInt16 => "ushort",
        PrimitiveTypeCode.Int32 => "int", PrimitiveTypeCode.UInt32 => "uint", PrimitiveTypeCode.Int64 => "long",
        PrimitiveTypeCode.UInt64 => "ulong", PrimitiveTypeCode.Single => "float", PrimitiveTypeCode.Double => "double",
        PrimitiveTypeCode.String => "string", PrimitiveTypeCode.Object => "object", PrimitiveTypeCode.Void => "void",
        PrimitiveTypeCode.IntPtr => "global::System.IntPtr", PrimitiveTypeCode.UIntPtr => "global::System.UIntPtr", _ => "!unknown" };
    public string GetSZArrayType(string t) => t + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) {
        var t=r.GetTypeDefinition(h); var p=t.GetDeclaringType();
        return p.IsNil ? "global::"+r.GetString(t.Namespace)+"."+r.GetString(t.Name) : GetTypeFromDefinition(r,p,k)+"."+r.GetString(t.Name);
    }
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) {
        var t=r.GetTypeReference(h);
        return t.ResolutionScope.Kind==HandleKind.TypeReference ? GetTypeFromReference(r,(TypeReferenceHandle)t.ResolutionScope,k)+"."+r.GetString(t.Name) : "global::"+r.GetString(t.Namespace)+"."+r.GetString(t.Name);
    }
    public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) => r.GetTypeSpecification(h).DecodeSignature(this,c);
    public sealed record Entry(string Method, string Owner, string[] Arguments);
    static string Canonical(string name) {
        var map=new Dictionary<string,string>{{"global::System.String","string"},{"global::System.Object","object"},{"global::System.Boolean","bool"},{"global::System.Int32","int"},{"global::System.UInt32","uint"},{"global::System.Int64","long"},{"global::System.UInt64","ulong"},{"global::System.Double","double"}};
        foreach(var pair in map)name=name.Replace(pair.Key,pair.Value);return name;
    }
    static string TypeName(MetadataReader r,HttpJsonContractInspection p,EntityHandle h) => h.Kind switch {
        HandleKind.TypeDefinition=>p.GetTypeFromDefinition(r,(TypeDefinitionHandle)h,0),
        HandleKind.TypeReference=>p.GetTypeFromReference(r,(TypeReferenceHandle)h,0),
        HandleKind.TypeSpecification=>p.GetTypeFromSpecification(r,null,(TypeSpecificationHandle)h,0),_=>""};
    static IEnumerable<(OpCode op,int token)> Instructions(byte[] bytes) {
        var ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>unchecked((ushort)o.Value));
        for(int i=0;i<bytes.Length;) {
            ushort key=bytes[i++];if(key==0xfe)key=(ushort)(0xfe00|bytes[i++]);var op=ops[key];int size=op.OperandType switch {
                OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
                OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,
                OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(bytes,i),_=>4};
            int token=size==4?BitConverter.ToInt32(bytes,i):0;yield return(op,token);i+=size;
        }
    }
    public static string[] Verify(string path) {
        using var stream=File.OpenRead(path);using var pe=new PEReader(stream);var r=pe.GetMetadataReader();var p=new HttpJsonContractInspection();var known=new HashSet<string>();var contexts=new HashSet<string>{"HttpApiJsonContext","MetadataFileJsonContext"};var found=new HashSet<string>();var registered=new HashSet<string>();
        foreach(var h in r.TypeDefinitions) {
            var t=r.GetTypeDefinition(h);string name=r.GetString(t.Name);
            if(!contexts.Contains(name)&&name!="JsonSerializeContexts")continue;
            if(contexts.Contains(name))found.Add(name);
            foreach(var mh in t.GetMethods()) {
                var method=r.GetMethodDefinition(mh);string mn=r.GetString(method.Name);if(method.RelativeVirtualAddress==0)continue;
                if(contexts.Contains(name)&&!mn.Contains("GetTypeInfo"))continue;
                if(name=="JsonSerializeContexts"&&mn!=".cctor")continue;
                foreach(var instruction in Instructions(pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().ToArray())) {
                    if(instruction.op==OpCodes.Ldtoken) {
                        var entity=MetadataTokens.EntityHandle(instruction.token);if(contexts.Contains(name))known.Add(Canonical(TypeName(r,p,entity)));
                    }
                    if(name=="JsonSerializeContexts"&&(instruction.op==OpCodes.Call||instruction.op==OpCodes.Callvirt)) {
                        var entity=MetadataTokens.EntityHandle(instruction.token);
                        if(entity.Kind==HandleKind.MethodDefinition){var m=r.GetMethodDefinition((MethodDefinitionHandle)entity);string target=r.GetString(r.GetTypeDefinition(m.GetDeclaringType()).Name);if(r.GetString(m.Name)=="get_Default"&&contexts.Contains(target))registered.Add(target);}
                        else if(entity.Kind==HandleKind.MemberReference){var m=r.GetMemberReference((MemberReferenceHandle)entity);string target=TypeName(r,p,m.Parent).Split('.').Last();if(r.GetString(m.Name)=="get_Default"&&contexts.Contains(target))registered.Add(target);}
                    }
                }
            }
        }
        foreach(var context in contexts){if(!found.Contains(context))throw new InvalidOperationException("Compiled assembly has no "+context+". Build current source before checking contracts.");if(!registered.Contains(context))throw new InvalidOperationException(context+" is not registered by JsonSerializeContexts.");}
        var expected=new HashSet<string>();
        foreach(var entry in Read(path)) {
            bool metadata=entry.Method=="FromCacheOrFileAsync"||entry.Method=="FromCacheAsDictionaryAsync";
            // Dictionary overloads use <TKey,TValue> or <TData,...>. Inspect
            // the actual file element type rather than the resulting map type.
            string type=entry.Method=="FromCacheAsDictionaryAsync"?(entry.Arguments.Length==2?entry.Arguments[1]:entry.Arguments[0]):entry.Method=="SendAsync"||metadata?entry.Arguments[0]:entry.Arguments.Length>1?entry.Arguments[entry.Arguments.Length-1]:"!open";
            if(!type.Contains("!")&&!type.Contains("NoContent")){expected.Add(Canonical(type));if(metadata)expected.Add(Canonical("global::System.Collections.Immutable.ImmutableArray<"+type+">"));}
        }
        return expected.Where(type=>!known.Contains(type)).OrderBy(t=>t).ToArray();
    }
    public static List<Entry> Read(string path) {
        using var stream=File.OpenRead(path); using var pe=new PEReader(stream); var r=pe.GetMetadataReader(); var p=new HttpJsonContractInspection(); var result=new List<Entry>();
        for(int i=1;i<=r.GetTableRowCount(TableIndex.MethodSpec);i++) {
            var spec=r.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(i)); string name; string owner="";
            if(spec.Method.Kind==HandleKind.MethodDefinition) { var m=r.GetMethodDefinition((MethodDefinitionHandle)spec.Method); name=r.GetString(m.Name); owner=p.GetTypeFromDefinition(r,m.GetDeclaringType(),0); }
            else {var m=r.GetMemberReference((MemberReferenceHandle)spec.Method);name=r.GetString(m.Name);if(m.Parent.Kind==HandleKind.TypeReference)owner=p.GetTypeFromReference(r,(TypeReferenceHandle)m.Parent,0);else if(m.Parent.Kind==HandleKind.TypeDefinition)owner=p.GetTypeFromDefinition(r,(TypeDefinitionHandle)m.Parent,0);}
            if(((owner.Contains("HttpRequestMessageBuilder") || owner.Contains("JsonBuilderExtension")) && (name.Contains("SendAsync")||name.Contains("PostJson")||name.Contains("PutJson")||name.Contains("PatchJson")||name.Contains("SetJsonContent"))) || (owner.Contains("MetadataService")&&(name.Contains("FromCacheOrFileAsync")||name.Contains("FromCacheAsDictionaryAsync")))) result.Add(new Entry(name,owner,spec.DecodeSignature(p,null).ToArray()));
        }
        return result;
    }
}
'@
$missing=[HttpJsonContractInspection]::Verify((Resolve-Path -LiteralPath $AssemblyPath).Path)
if($missing.Count -gt 0){$missing|ForEach-Object {Write-Output "Missing HTTP JSON contract: $_"};throw "$($missing.Count) typed HTTP contracts lack generated metadata."}
Write-Output 'PASS: compiled HTTP and metadata-file contracts have generated JSON metadata and their resolvers are registered.'
