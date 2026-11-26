using Microsoft.UI.Xaml;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Projection;
using Windows.Foundation;

namespace Snap.Hutao.Plugin.SDK;

public sealed partial class PluginContext : IDisposable
{
    private readonly IPluginContext abi;
    private readonly List<IPluginRegistration> registrations = [];
    private bool disposed;

    internal PluginContext(IPluginContext abi)
    {
        this.abi = abi;
    }

    public string PluginId => abi.PluginId;
    public string PluginName => abi.PluginName;
    public string PluginVersion => abi.PluginVersion;
    public string PackageDirectory => abi.PackageDirectory;
    public string DataDirectory => abi.DataDirectory;
    public uint ApiMinor => abi.ApiMinor;
    public PluginSettings Settings => new(this, abi.Settings);
    public bool HasThreadAccess => abi.Dispatcher.HasThreadAccess;

    public void Log(NotificationKind kind, string message) => abi.Log(kind, message);
    public void Notify(NotificationKind kind, string message) => abi.Notifications.Show(kind, message);
    public UIElement LoadXaml(string relativePath) => abi.Ui.LoadXaml(relativePath);
    public UIElement ParseXaml(string markup) => abi.Ui.ParseXaml(markup);
    public async Task<DialogResult> ShowMessageAsync(string title, string message, string primaryText = "OK", string secondaryText = "Cancel")
        => await abi.Dialogs.ShowMessageAsync(title, message, primaryText, secondaryText);

    // Use for external UI events that do not enter through a generated SDK adapter.
    // This restores the plugin runtime synchronization context across awaits.
    public Task RunOnUiAsync(Func<Task> callback)
    {
        if (!HasThreadAccess) throw new InvalidOperationException("Expected plugin UI thread.");
        return UiCall.Run(callback);
    }

    public async Task InvokeAsync(Action callback)
    {
        IPluginDispatcher dispatcher = abi.Dispatcher;
        ActionAdapter action = new(callback);
        PluginAbi.RequireAgile(dispatcher);
        PluginAbi.RequireAgile<IPluginAction>(action);
        await dispatcher.InvokeAsync(action);
    }

    // Capture on the UI thread before passing to a worker.
    public PluginDispatcher CaptureDispatcher() => new(abi.Dispatcher);

    public IDisposable RegisterView(string id, Func<PluginView> factory)
        => Track(abi.Contributions.RegisterView(id, new ViewFactoryAdapter(factory)));

    public IDisposable RegisterNavigationItem(string id, string title, string iconGlyph, string viewId)
        => Track(abi.Contributions.RegisterNavigationItem(id, title, iconGlyph, viewId));

    public IDisposable RegisterCommand(string id, string title, Func<Task> execute)
        => Track(abi.Contributions.RegisterCommand(id, title, new CommandAdapter(execute)));

    internal IDisposable Track(IPluginRegistration registration)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        registrations.Add(registration);
        return new RegistrationHandle(registration);
    }

    internal void DisposeRegistrations()
    {
        List<Exception> errors = [];
        for (int i = registrations.Count - 1; i >= 0; --i)
        {
            try { registrations[i].Revoke(); }
            catch (Exception error) { errors.Add(error); }
        }
        registrations.Clear();
        if (errors.Count != 0) throw new AggregateException(errors);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        DisposeRegistrations();
    }
}

public sealed class PluginDispatcher
{
    private readonly IPluginDispatcher dispatcher;
    internal PluginDispatcher(IPluginDispatcher dispatcher)
    {
        PluginAbi.RequireAgile(dispatcher);
        this.dispatcher = dispatcher;
    }
    public bool HasThreadAccess => dispatcher.HasThreadAccess;
    public async Task InvokeAsync(Action callback)
    {
        ActionAdapter action = new(callback);
        PluginAbi.RequireAgile<IPluginAction>(action);
        await dispatcher.InvokeAsync(action);
    }
}

public sealed class PluginView(UIElement element, Action? onClosing = null)
{
    public UIElement Element { get; } = element;
    internal void Close() => onClosing?.Invoke();
}

internal sealed partial class ViewAdapter(PluginView view) : IPluginView
{
    private bool closed;
    public UIElement Element => view.Element;
    public void OnClosing()
    {
        if (closed) return;
        closed = true;
        UiCall.Run(view.Close);
    }
}

internal sealed partial class ViewFactoryAdapter(Func<PluginView> factory) : IPluginViewFactory
{
    public IPluginView CreateView(string viewId) => new ViewAdapter(UiCall.Run(factory));
}

internal sealed partial class ActionAdapter(Action callback) : IPluginAction
{
    public void Invoke() => UiCall.Run(callback);
}

internal sealed partial class CommandAdapter(Func<Task> execute) : IPluginCommand
{
    public IAsyncAction ExecuteAsync(string commandId) => UiCall.Run(execute).AsAsyncAction();
}

internal sealed partial class RegistrationHandle(IPluginRegistration registration) : IDisposable
{
    private IPluginRegistration? value = registration;
    public void Dispose() => Interlocked.Exchange(ref value, null)?.Revoke();
}
