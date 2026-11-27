using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Snap.Hutao.Plugin.SDK;

namespace TestPlugin.Aot;

public static unsafe class XamlExports
{
    [UnmanagedCallersOnly(EntryPoint = "DllGetActivationFactory", CallConvs = [typeof(CallConvStdcall)])]
    public static int GetActivationFactory(nint classId, nint* factory)
        => PluginXamlExport.GetActivationFactory<Plugin_XamlTypeInfo.XamlMetaDataProvider>(classId, factory);
}
