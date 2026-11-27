using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Markup;
using Snap.Hutao.Plugin.Projection;
using WinRT;
using WinRT.Interop;

namespace Snap.Hutao.Plugin.SDK;

public static unsafe class PluginXamlExport
{
    private const int ClassNotAvailable = unchecked((int)0x80040111);

    // Call from a Stdcall DllGetActivationFactory export in the plugin's native DLL.
    public static int GetActivationFactory<TProvider>(nint classId, nint* factory)
        where TProvider : class, IXamlMetadataProvider, new()
    {
        if (factory == null) return PluginAbi.PointerError;
        *factory = 0;
        if (classId == 0) return ClassNotAvailable;

        try
        {
            if (!string.Equals(MarshalString.FromAbi(classId), typeof(TProvider).FullName, StringComparison.Ordinal))
                return ClassNotAvailable;

            ComWrappersSupport.InitializeComWrappers();
            XamlActivationFactory instance = new(static () => new TProvider());
            *factory = MarshalInspectable<XamlActivationFactory>.CreateMarshaler2(instance, global::ABI.WinRT.Interop.IActivationFactoryMethods.IID).Detach();
            return 0;
        }
        catch (Exception error)
        {
            return error.HResult < 0 ? error.HResult : unchecked((int)0x80004005);
        }
    }

    [WinRTExposedType(typeof(XamlActivationFactoryTypeDetails))]
    private sealed class XamlActivationFactory(Func<IXamlMetadataProvider> create) : IActivationFactory
    {
        public nint ActivateInstance() => MarshalInspectable<IXamlMetadataProvider>.FromManaged(create());
    }

    private sealed class XamlActivationFactoryTypeDetails : IWinRTExposedTypeDetails
    {
        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces() =>
        [
            new()
            {
                IID = global::ABI.WinRT.Interop.IActivationFactoryMethods.IID,
                Vtable = global::ABI.WinRT.Interop.IActivationFactoryMethods.AbiToProjectionVftablePtr,
            },
        ];
    }
}
