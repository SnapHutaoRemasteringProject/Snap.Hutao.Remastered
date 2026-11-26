using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Projection;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace PrototypeHost;

// Isolated test adapter, not the production service or settings store.
internal sealed partial class ProbeContext(Window window, string dllPath, Action<string> write) :
    IPluginContext, IPluginSettings, IPluginNotifications, IPluginDispatcher, IPluginDialogs, IPluginContributions, IPluginUi
{
    private readonly DispatcherQueue queue = window.DispatcherQueue;
    private readonly Dictionary<string, (object Default, object Value)> values = new(StringComparer.Ordinal);
    private readonly List<IPluginSettingObserver> observers = [];
    public IPluginViewFactory? ViewFactory { get; private set; }
    public IPluginCommand? Command { get; private set; }
    public bool NavigationRegistered { get; private set; }
    public bool HasContributions => ViewFactory is not null || Command is not null || NavigationRegistered;
    public string PluginId { get { CheckThread(); return Path.GetFileNameWithoutExtension(dllPath); } }
    public string PluginName => PluginId;
    public string PluginVersion { get { CheckThread(); return "1.0.0"; } }
    public string PackageDirectory { get { CheckThread(); return Path.GetDirectoryName(dllPath)!; } }
    public string DataDirectory => PackageDirectory;
    public uint AbiMajor { get { CheckThread(); return PluginAbi.Major; } }
    public uint ApiMinor { get { CheckThread(); return 0; } }
    public IPluginSettings Settings { get { CheckThread(); return this; } }
    public IPluginNotifications Notifications { get { CheckThread(); return this; } }
    public IPluginDispatcher Dispatcher { get { CheckThread(); return this; } }
    public IPluginDialogs Dialogs { get { CheckThread(); return this; } }
    public IPluginContributions Contributions { get { CheckThread(); return this; } }
    public IPluginUi Ui { get { CheckThread(); return this; } }

    public object? GetService(Guid id)
    {
        CheckThread();
        if (id == typeof(IPluginSettings).GUID || id == typeof(IPluginNotifications).GUID ||
            id == typeof(IPluginDispatcher).GUID || id == typeof(IPluginDialogs).GUID ||
            id == typeof(IPluginContributions).GUID || id == typeof(IPluginUi).GUID) return this;
        return null;
    }

    public bool HasThreadAccess => queue.HasThreadAccess;
    private void CheckThread()
    {
        if (!HasThreadAccess) throw new COMException("Expected UI STA.", unchecked((int)0x8001010E));
    }

    public void Log(NotificationKind severity, string message) { CheckThread(); write($"[{severity}] {message}"); }
    public void Show(NotificationKind kind, string message) => Log(kind, message);

    public IAsyncAction InvokeAsync(IPluginAction action)
    {
        PluginAbi.RequireAgile(action);
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(() =>
        {
            try { action.Invoke(); completion.SetResult(); }
            catch (Exception ex) { completion.SetException(ex); }
        })) completion.SetException(new COMException("Dispatcher closed.", unchecked((int)0x80000013)));
        return completion.Task.AsAsyncAction();
    }

    public IAsyncOperation<DialogResult> ShowMessageAsync(string title, string message, string primaryText, string secondaryText)
    {
        CheckThread();
        return ShowDialogAsync(title, message, primaryText, secondaryText).AsAsyncOperation();
    }

    private async Task<DialogResult> ShowDialogAsync(string title, string message, string primaryText, string secondaryText)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = window.Content.XamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = primaryText,
            SecondaryButtonText = secondaryText,
            CloseButtonText = "Close"
        };
        return (await dialog.ShowAsync()) switch
        {
            ContentDialogResult.Primary => DialogResult.Primary,
            ContentDialogResult.Secondary => DialogResult.Secondary,
            _ => DialogResult.None
        };
    }

    public IPluginRegistration RegisterView(string viewId, IPluginViewFactory factory)
    {
        CheckThread();
        if (viewId != "main" || ViewFactory is not null) throw new ArgumentException(nameof(viewId));
        ViewFactory = factory;
        return new Registration(() => { CheckThread(); ViewFactory = null; });
    }

    public IPluginRegistration RegisterNavigationItem(string itemId, string title, string iconGlyph, string viewId)
    {
        CheckThread();
        if (itemId != "main" || viewId != "main" || ViewFactory is null || NavigationRegistered)
            throw new ArgumentException(nameof(itemId));
        NavigationRegistered = true;
        write("Registered navigation: " + title);
        return new Registration(() => { CheckThread(); NavigationRegistered = false; });
    }

    public IPluginRegistration RegisterCommand(string commandId, string title, IPluginCommand command)
    {
        CheckThread();
        if (commandId != "exercise" || Command is not null) throw new ArgumentException(nameof(commandId));
        Command = command;
        return new Registration(() => { CheckThread(); Command = null; });
    }

    public UIElement LoadXaml(string relativePath)
    {
        CheckThread();
        if (Path.GetFileName(relativePath) != relativePath || !relativePath.EndsWith(".hxaml", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(nameof(relativePath));
        return ParseXaml(File.ReadAllText(Path.Combine(PackageDirectory, relativePath)));
    }

    public UIElement ParseXaml(string markup)
    {
        CheckThread();
        return XamlReader.Load(markup) as UIElement ?? throw new ArgumentException("Expected UIElement.");
    }

    private void Register(string name, object value)
    {
        CheckThread();
        if (!values.TryAdd(name, (value, value))) throw new ArgumentException("Duplicate setting.");
    }

    private T Get<T>(string name)
    {
        CheckThread();
        if (!values.TryGetValue(name, out var item) || item.Value is not T value) throw new ArgumentException("Unknown setting or type mismatch.");
        return value;
    }

    private IAsyncAction Set<T>(string name, T value) where T : notnull
    {
        _ = Get<T>(name);
        values[name] = (values[name].Default, value);
        foreach (var observer in observers.ToArray()) observer.OnChanged(name);
        return Task.CompletedTask.AsAsyncAction();
    }

    public void RegisterInt32(string name, int defaultValue, string description) => Register(name, defaultValue);
    public void RegisterBoolean(string name, bool defaultValue, string description) => Register(name, defaultValue);
    public void RegisterString(string name, string defaultValue, string description) => Register(name, defaultValue);
    public int GetInt32(string name) => Get<int>(name);
    public bool GetBoolean(string name) => Get<bool>(name);
    public string GetString(string name) => Get<string>(name);
    public IAsyncAction SetInt32Async(string name, int newValue) => Set(name, newValue);
    public IAsyncAction SetBooleanAsync(string name, bool newValue) => Set(name, newValue);
    public IAsyncAction SetStringAsync(string name, string newValue) => Set(name, newValue);
    public IAsyncAction ResetAsync(string name)
    {
        CheckThread();
        return values[name].Default switch
        {
            int i => Set(name, i),
            bool b => Set(name, b),
            string s => Set(name, s),
            _ => throw new ArgumentException(nameof(name))
        };
    }

    public IPluginRegistration Subscribe(IPluginSettingObserver observer)
    {
        CheckThread();
        observers.Add(observer);
        return new Registration(() => { CheckThread(); observers.Remove(observer); });
    }
}

internal sealed partial class Registration(Action revoke) : IPluginRegistration
{
    private Action? callback = revoke;
    public void Revoke() => Interlocked.Exchange(ref callback, null)?.Invoke();
}
