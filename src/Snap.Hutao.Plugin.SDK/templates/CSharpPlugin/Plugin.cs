using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.SDK;

public static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = "HutaoPluginCreate", CallConvs = [typeof(CallConvCdecl)])]
    public static int Create(uint abiMajor, nint* result) => PluginExport.Create<MyPlugin>(abiMajor, result);
}

public sealed class MyPlugin : HutaoPlugin
{
    public override void OnLoad(PluginContext context) => context.Log(NotificationKind.Information, "Loaded");

    public override void OnEnable()
    {
        Context.RegisterView("main", CreateView);
        Context.RegisterNavigationItem("main", "My Plugin", "\uE74C", "main");
    }

    private PluginView CreateView()
    {
        Button button = new() { Content = "Click me" };
        void Click(object sender, RoutedEventArgs args) => Context.Notify(NotificationKind.Success, "Hello from plugin");
        button.Click += Click;
        return new(button, () => button.Click -= Click);
    }
}
