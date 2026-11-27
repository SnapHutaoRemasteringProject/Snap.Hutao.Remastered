using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Projection;
using WinRT;

namespace Snap.Hutao.Plugin.SDK;

public abstract class HutaoPlugin
{
    private PluginContext? context;
    public PluginContext Context => context ?? throw new InvalidOperationException("Plugin is not loaded.");

    public abstract void OnLoad(PluginContext context);
    public virtual void OnInstall() { }
    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnUninstall() { }

    internal void Attach(PluginContext value) => context = value;
    internal void Detach() => context = null;
}

// One ABI adapter per instance. Developer classes do not need to implement WinRT interfaces.
internal sealed partial class PluginAdapter(HutaoPlugin plugin) : IHutaoPlugin
{
    public void OnLoad(IPluginContext value)
    {
        if (value.AbiMajor != PluginAbi.Major) throw new COMException("ABI mismatch.", PluginAbi.RevisionMismatch);
        PluginContext context = new(value);
        plugin.Attach(context);
        try { UiCall.Run(() => plugin.OnLoad(context)); }
        catch { context.Dispose(); plugin.Detach(); throw; }
    }

    public void OnInstall() => UiCall.Run(plugin.OnInstall);
    public void OnEnable() => UiCall.Run(plugin.OnEnable);
    public void OnDisable()
    {
        try { UiCall.Run(plugin.OnDisable); }
        finally { plugin.Context.DisposeRegistrations(); }
    }
    public void OnUninstall()
    {
        try { UiCall.Run(plugin.OnUninstall); }
        finally { plugin.Context.Dispose(); plugin.Detach(); }
    }
}

// Separate Native AOT runtimes do not share managed SynchronizationContext.
internal static class UiCall
{
    public static T Run<T>(Func<T> callback)
    {
        DispatcherQueue? queue = DispatcherQueue.GetForCurrentThread();
        return queue is null ? callback() : RunWithContext(new PluginUiSynchronizationContext(queue), callback);
    }

    public static void Run(Action callback) => Run(() => { callback(); return true; });

    private static T RunWithContext<T>(SynchronizationContext context, Func<T> callback)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try { return callback(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    // DispatcherQueue is native and shared across runtimes; managed Current is not.
    // Install the plugin runtime's context for every dispatched continuation, not just
    // the initial ABI callback, so consecutive awaits keep targeting the UI thread.
    private sealed class PluginUiSynchronizationContext(DispatcherQueue queue) : SynchronizationContext
    {
        public override SynchronizationContext CreateCopy() => this;

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (!queue.TryEnqueue(() => RunWithContext(this, () => { d(state); return true; })))
                throw new InvalidOperationException("Plugin UI dispatcher is closed.");
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (!queue.HasThreadAccess)
                throw new NotSupportedException("Synchronous cross-thread plugin dispatch is not supported.");
            RunWithContext(this, () => { d(state); return true; });
        }
    }
}

public static unsafe class PluginExport
{
    // Call from the plugin assembly's HutaoPluginCreate export.
    public static int Create<T>(uint abiMajor, nint* result) where T : HutaoPlugin, new()
    {
        if (result == null) return PluginAbi.PointerError;
        *result = 0;
        if (abiMajor != PluginAbi.Major) return PluginAbi.RevisionMismatch;
        try
        {
            ComWrappersSupport.InitializeComWrappers();
            *result = PluginAbi.Export(new PluginAdapter(new T()));
            return 0;
        }
        catch (Exception ex) { return ex.HResult < 0 ? ex.HResult : unchecked((int)0x80004005); }
    }
}
