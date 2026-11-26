using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Projection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WinRT;

namespace TestPlugin.Aot;

public static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = "HutaoPluginCreate", CallConvs = [typeof(CallConvCdecl)])]
    public static int Create(uint abiMajor, nint* result)
    {
        if (result == null) return PluginAbi.PointerError;
        *result = 0;
        if (abiMajor != PluginAbi.Major) return PluginAbi.RevisionMismatch;
        try
        {
            ComWrappersSupport.InitializeComWrappers();
            *result = PluginAbi.Export(new TestPlugin());
            return 0;
        }
        catch (Exception ex) { return ex.HResult < 0 ? ex.HResult : unchecked((int)0x80004005); }
    }
}

internal sealed partial class TestPlugin : IHutaoPlugin, IPluginViewFactory, IPluginCommand, IPluginAction
{
    private IPluginContext? context;
    private readonly List<IPluginRegistration> registrations = [];
    private IPluginContext Context => context ?? throw new InvalidOperationException("Not loaded.");

    public void OnLoad(IPluginContext value)
    {
        context = value;
        if (value.AbiMajor != 1 || value.ApiMinor != 0) throw new InvalidOperationException("ABI mismatch.");
        value.Settings.RegisterInt32("Counter", 0, "Counter");
        value.Settings.RegisterBoolean("Enabled", true, "Boolean");
        value.Settings.RegisterString("Message", "Default Message", "Message");
        object service = value.GetService(typeof(IPluginNotifications).GUID) ?? throw new InvalidOperationException("Service missing.");
        IPluginNotifications notifications = service.As<IPluginNotifications>();
        notifications.Show(NotificationKind.Success, "C# AOT OnLoad: " + value.PluginId);
        if (value.GetService(Guid.Empty) is not null) throw new InvalidOperationException("Unknown service must be null.");
    }

    public void OnInstall() => Context.Log(NotificationKind.Information, "C# AOT OnInstall");
    public void OnEnable()
    {
        registrations.Add(Context.Contributions.RegisterView("main", this));
        registrations.Add(Context.Contributions.RegisterNavigationItem("main", "C# AOT plugin", "\uE74C", "main"));
        registrations.Add(Context.Contributions.RegisterCommand("exercise", "Exercise ABI", this));
    }

    public void OnDisable()
    {
        foreach (var registration in registrations) registration.Revoke();
        registrations.Clear();
    }

    public void OnUninstall()
    {
        Context.Log(NotificationKind.Information, "C# AOT OnUninstall");
        context = null;
    }

    public IPluginView CreateView(string viewId) => viewId == "main" ? new TestView(Context) : throw new ArgumentException(nameof(viewId));
    public IAsyncAction ExecuteAsync(string commandId)
    {
        // This DLL has its own .NET runtime: the host's managed SynchronizationContext
        // is not shared. Capture this runtime's own UI dispatcher for async continuations.
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
        try { return ExerciseAsync(commandId).AsAsyncAction(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private async Task ExerciseAsync(string commandId)
    {
        if (commandId != "exercise") throw new ArgumentException(nameof(commandId));
        var host = Context;
        await host.Settings.SetInt32Async("Counter", host.Settings.GetInt32("Counter") + 1);
        await host.Settings.SetBooleanAsync("Enabled", false);
        await host.Settings.SetStringAsync("Message", "C# AOT round trip");
        if (host.Settings.GetBoolean("Enabled") || host.Settings.GetString("Message") != "C# AOT round trip")
            throw new InvalidOperationException("Settings round trip failed.");
        var dispatcher = host.Dispatcher;
        PluginAbi.RequireAgile(dispatcher);
        PluginAbi.RequireAgile<IPluginAction>(this);
        await Task.Run(async () =>
        {
            if (dispatcher.HasThreadAccess) throw new InvalidOperationException("Expected worker.");
            await dispatcher.InvokeAsync(this);
        });
        var result = await host.Dialogs.ShowMessageAsync("C# AOT ABI", "Choose OK to complete the test.", "OK", "Cancel");
        if (result != DialogResult.Primary) throw new OperationCanceledException();
        host.Notifications.Show(NotificationKind.Success, "C# AOT async round trip completed.");
    }

    public void Invoke()
    {
        if (!Context.Dispatcher.HasThreadAccess) throw new InvalidOperationException("Expected UI thread.");
        Context.Log(NotificationKind.Information, "C# AOT worker callback on UI thread");
    }
}

internal sealed partial class TestView : IPluginView
{
    private readonly IPluginContext context;
    private readonly Button button = new() { Content = "Click C# AOT plugin-owned Button" };
    public TestView(IPluginContext context)
    {
        this.context = context;
        button.Click += Click;
    }

    public UIElement Element => button;

    private void Click(object sender, RoutedEventArgs args)
    {
        button.Content = "C# AOT plugin click executed";
        context.Notifications.Show(NotificationKind.Success, "C# AOT UI callback");
    }

    public void OnClosing() => button.Click -= Click;
}
