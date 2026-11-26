using System.IO;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Packaging;
using Snap.Hutao.Plugin.Projection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Foundation;

namespace Snap.Hutao.Remastered.Service.Plugin;

public sealed record PluginIdentity(string Id, string Name, string Version, string PackageDirectory, string DataDirectory,
    string SettingsDirectory, string UiMode, IReadOnlySet<string> Capabilities, IReadOnlySet<string> Files, string? MetadataProvider = null);

// Host owns this context. Do not expose the underlying application IServiceProvider to plugins.
internal sealed partial class PluginHostContext : IPluginContext, IPluginSettings, IPluginNotifications, IPluginDispatcher,
    IPluginDialogs, IPluginContributions, IPluginUi
{
    private enum Phase { Loading, Installing, Enabling, Active, Closing, Closed }
    private Phase phase = Phase.Loading;
    private readonly PluginIdentity identity;
    private readonly DispatcherQueue queue;
    private readonly Func<XamlRoot?> xamlRoot;
    private readonly Action<NotificationKind, string> log;
    private readonly Action<NotificationKind, string> notify;
    private readonly Dictionary<string, (object Default, object Value)> values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonElement> snapshot;
    private readonly Dictionary<string, IPluginViewFactory> views = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string Title, IPluginCommand Command)> commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> descriptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NavigationContribution> navigation = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IPluginRegistration> tokens = [];
    private readonly List<IPluginSettingObserver> observers = [];
    private readonly List<(NotificationKind Kind, string Message)> pendingNotifications = [];
    private readonly HashSet<Task> pending = [];
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private ContentDialog? dialog;

    internal PluginHostContext(PluginIdentity identity, DispatcherQueue queue, Func<XamlRoot?> xamlRoot,
        Action<NotificationKind, string> log, Action<NotificationKind, string> notify, Dictionary<string, JsonElement> snapshot)
    {
        this.identity = identity;
        this.queue = queue;
        this.xamlRoot = xamlRoot;
        this.log = log;
        this.notify = notify;
        this.snapshot = snapshot;
    }

    public string PluginId { get { Check(); return identity.Id; } }
    public string PluginName { get { Check(); return identity.Name; } }
    public string PluginVersion { get { Check(); return identity.Version; } }
    public string PackageDirectory { get { Check(); return identity.PackageDirectory; } }
    public string DataDirectory { get { Check(); return identity.DataDirectory; } }
    public uint AbiMajor { get { Check(); return PluginAbi.Major; } }
    public uint ApiMinor { get { Check(); return 0; } }
    public IPluginSettings Settings { get { Demand("settings"); return this; } }
    public IPluginNotifications Notifications { get { Demand("notifications"); return this; } }
    public IPluginDispatcher Dispatcher { get { Check(); return this; } }
    public IPluginDialogs Dialogs { get { Demand("dialogs"); return this; } }
    public IPluginContributions Contributions { get { Check(); return this; } }
    public IPluginUi Ui { get { Demand("xaml"); return this; } }

    public object? GetService(Guid id)
    {
        Check();
        // Every service method enforces its capability as well: QI is not a permission boundary.
        if (id == typeof(IPluginSettings).GUID && Allowed("settings") ||
            id == typeof(IPluginNotifications).GUID && Allowed("notifications") ||
            id == typeof(IPluginDispatcher).GUID ||
            id == typeof(IPluginDialogs).GUID && Allowed("dialogs") ||
            id == typeof(IPluginContributions).GUID && (Allowed("navigation") || Allowed("commands")) ||
            id == typeof(IPluginUi).GUID && Allowed("xaml")) return this;
        return null;
    }

    public bool HasThreadAccess => queue.HasThreadAccess;
    private void CheckThread()
    {
        if (!HasThreadAccess) throw new COMException("Expected UI STA.", unchecked((int)0x8001010E));
    }
    private void Check()
    {
        CheckThread();
        if (phase == Phase.Closed) throw new COMException("Plugin session closed.", unchecked((int)0x80000013));
    }
    private bool Allowed(string capability) => identity.Capabilities.Contains(capability);
    private void Demand(string capability)
    {
        Check();
        if (!Allowed(capability)) throw new UnauthorizedAccessException("Plugin capability not granted: " + capability);
    }
    private void Active()
    {
        Check();
        if (phase != Phase.Active) throw new InvalidOperationException("Plugin session is not active.");
    }
    private void Registering()
    {
        Check();
        if (phase is not (Phase.Loading or Phase.Enabling)) throw new InvalidOperationException("Contribution registration is not allowed in this phase.");
    }

    internal void BeginInstall() { Check(); phase = Phase.Installing; }
    internal void BeginEnable() { Check(); phase = Phase.Enabling; }
    internal void ValidateContributions()
    {
        Check();
        foreach (var item in navigation.Values)
            if (!views.ContainsKey(item.ViewId)) throw new InvalidOperationException("Unknown navigation view: " + item.ViewId);
    }
    internal void Commit()
    {
        ValidateContributions();
        phase = Phase.Active;
        foreach (var item in pendingNotifications)
        {
            try { notify(item.Kind, item.Message); }
            catch (Exception error) { log(NotificationKind.Error, error.ToString()); }
        }
        pendingNotifications.Clear();
    }

    internal IReadOnlyList<NavigationContribution> Navigation
    {
        get { Active(); return navigation.Values.ToArray(); }
    }
    internal IReadOnlyList<CommandContribution> Commands
    {
        get { Active(); return commands.Select(item => new CommandContribution(item.Key, item.Value.Title)).ToArray(); }
    }

    internal IReadOnlyList<RegisteredPluginSetting> SettingsSnapshot
    {
        get { Active(); return values.Select(item => new RegisteredPluginSetting(item.Key, descriptions[item.Key], item.Value.Default, item.Value.Value)).ToArray(); }
    }

    internal Task SetSettingAsync(string name, object value) => value switch
    {
        int number => SetInt32Async(name, number).AsTask(),
        bool flag => SetBooleanAsync(name, flag).AsTask(),
        string text => SetStringAsync(name, text).AsTask(),
        _ => throw new ArgumentException("Unsupported setting type.", nameof(value)),
    };

    internal IPluginView OpenView(string id)
    {
        Active();
        return views.TryGetValue(id, out var factory) ? factory.CreateView(id) : throw new KeyNotFoundException(id);
    }
    internal IAsyncAction Execute(string id)
    {
        Active();
        return commands.TryGetValue(id, out var command) ? command.Command.ExecuteAsync(id) : throw new KeyNotFoundException(id);
    }
    internal void BeginClose()
    {
        Check();
        phase = Phase.Closing;
        dialog?.Hide();
    }
    internal Task DrainAsync() { Check(); return Task.WhenAll(pending.ToArray()); }
    internal void Close()
    {
        CheckThread();
        if (phase == Phase.Closed) return;
        phase = Phase.Closing;
        foreach (var token in tokens.ToArray().Reverse())
        {
            try { token.Revoke(); }
            catch (Exception error) { log(NotificationKind.Error, error.ToString()); }
        }
        tokens.Clear(); observers.Clear(); views.Clear(); commands.Clear(); navigation.Clear();
        pendingNotifications.Clear();
        phase = Phase.Closed;
    }

    public void Log(NotificationKind severity, string message) { Check(); log(severity, message); }
    public void Show(NotificationKind kind, string message)
    {
        Demand("notifications");
        if (phase == Phase.Active) notify(kind, message);
        else if (phase is Phase.Loading or Phase.Installing or Phase.Enabling) pendingNotifications.Add((kind, message));
        else throw new InvalidOperationException("Notifications are not allowed while closing.");
    }

    // Queue callback execution only while the original session remains active.
    public IAsyncAction InvokeAsync(IPluginAction action)
    {
        PluginAbi.RequireAgile(action);
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(() =>
        {
            try { Active(); action.Invoke(); completion.SetResult(); }
            catch (Exception ex) { completion.SetException(ex); }
        })) completion.SetException(new COMException("Dispatcher closed.", unchecked((int)0x80000013)));
        return completion.Task.AsAsyncAction();
    }

    public IAsyncOperation<DialogResult> ShowMessageAsync(string title, string message, string primaryText, string secondaryText)
    {
        Demand("dialogs"); Active();
        return Track(ShowDialogAsync(title, message, primaryText, secondaryText)).AsAsyncOperation();
    }
    private async Task<DialogResult> ShowDialogAsync(string title, string message, string primaryText, string secondaryText)
    {
        if (dialog is not null) throw new InvalidOperationException("A plugin dialog is already open.");
        ContentDialog current = new()
        {
            XamlRoot = xamlRoot() ?? throw new InvalidOperationException("Host XamlRoot unavailable."),
            Title = title, Content = message, PrimaryButtonText = primaryText, SecondaryButtonText = secondaryText, CloseButtonText = "Close"
        };
        dialog = current;
        try
        {
            return (await current.ShowAsync()) switch
            {
                ContentDialogResult.Primary => DialogResult.Primary,
                ContentDialogResult.Secondary => DialogResult.Secondary,
                _ => DialogResult.None
            };
        }
        finally { dialog = null; }
    }
    private Task<T> Track<T>(Task<T> task) { _ = TrackTask(task); return task; }
    private Task Track(Task task) { _ = TrackTask(task); return task; }
    private async Task TrackTask(Task task)
    {
        pending.Add(task);
        try { await task; }
        catch { /* Original task reports the error to its caller. */ }
        finally { pending.Remove(task); }
    }
    private IPluginRegistration RegisterToken(Action revoke)
    {
        Registration token = new(() => { CheckThread(); revoke(); });
        tokens.Add(token);
        return token;
    }
    private static void ValidateName(string name)
    {
        PackagePaths.ValidateIdentifier(name);
        if (name.Length > 64) throw new ArgumentException("Local identifier exceeds 64 characters.");
    }
    private static void ValidateTitle(string title)
    {
        if (title.Length is < 1 or > 128) throw new ArgumentException("Invalid contribution title.");
    }

    public IPluginRegistration RegisterView(string viewId, IPluginViewFactory factory)
    {
        Demand("navigation"); Registering(); ValidateName(viewId);
        if (identity.UiMode == "none") throw new InvalidOperationException("UI is not declared.");
        if (views.Count >= 16) throw new InvalidOperationException("Too many views.");
        if (!views.TryAdd(viewId, factory)) throw new ArgumentException("Duplicate view.", nameof(viewId));
        return RegisterToken(() => views.Remove(viewId));
    }
    public IPluginRegistration RegisterNavigationItem(string itemId, string title, string iconGlyph, string viewId)
    {
        Demand("navigation"); Registering(); ValidateName(itemId); ValidateName(viewId);
        if (identity.UiMode == "none") throw new InvalidOperationException("UI is not declared.");
        if (navigation.Count >= 16) throw new InvalidOperationException("Too many navigation items.");
        ValidateTitle(title);
        int glyphCount = 0;
        foreach (var rune in iconGlyph.EnumerateRunes())
            if (++glyphCount > 1) throw new ArgumentException("Expected one icon glyph.");
        if (!navigation.TryAdd(itemId, new(itemId, title, iconGlyph, viewId))) throw new ArgumentException("Duplicate navigation.", nameof(itemId));
        return RegisterToken(() => navigation.Remove(itemId));
    }
    public IPluginRegistration RegisterCommand(string commandId, string title, IPluginCommand command)
    {
        Demand("commands"); Registering(); ValidateName(commandId);
        if (commands.Count >= 64) throw new InvalidOperationException("Too many commands.");
        ValidateTitle(title);
        if (!commands.TryAdd(commandId, (title, command))) throw new ArgumentException("Duplicate command.", nameof(commandId));
        return RegisterToken(() => commands.Remove(commandId));
    }

    public UIElement LoadXaml(string relativePath)
    {
        Demand("xaml"); Active();
        if (identity.UiMode != "loose") throw new UnauthorizedAccessException("Loose XAML not declared.");
        PackagePaths.ValidateRelativePath(relativePath);
        if (!relativePath.EndsWith(".hxaml", StringComparison.OrdinalIgnoreCase) || !identity.Files.Contains(relativePath))
            throw new UnauthorizedAccessException("XAML must be listed in the verified manifest.");
        string path = PackagePaths.Resolve(identity.PackageDirectory, relativePath);
        PackagePaths.RejectReparseAncestors(path);
        using FileStream stream = File.OpenRead(path);
        if (stream.Length > 1024 * 1024) throw new InvalidDataException("XAML exceeds 1 MiB.");
        using StreamReader reader = new(stream, new UTF8Encoding(false, true));
        return ParseXaml(reader.ReadToEnd());
    }
    [WinRT.DynamicWindowsRuntimeCast(typeof(UIElement))]
    public UIElement ParseXaml(string markup)
    {
        Demand("xaml"); Active();
        if (identity.UiMode != "loose") throw new UnauthorizedAccessException("Loose XAML not declared.");
        if (Encoding.UTF8.GetByteCount(markup) > 1024 * 1024) throw new InvalidDataException("XAML exceeds 1 MiB.");
        return XamlReader.Load(markup) as UIElement ?? throw new ArgumentException("Expected UIElement.");
    }

    private void Register(string name, object value, string description)
    {
        Demand("settings"); ValidateName(name);
        if (phase is not (Phase.Loading or Phase.Installing or Phase.Enabling)) throw new InvalidOperationException("Setting registration is not allowed.");
        object stored = value;
        if (snapshot.TryGetValue(name, out var json))
        {
            if (value is int && json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out int number)) stored = number;
            else if (value is bool && json.ValueKind is JsonValueKind.True or JsonValueKind.False) stored = json.GetBoolean();
            else if (value is string && json.ValueKind == JsonValueKind.String) stored = json.GetString()!;
            else log(NotificationKind.Warning, "Invalid saved setting; using default: " + name);
        }
        if (values.Count >= 128) throw new InvalidOperationException("Too many settings.");
        if (!values.TryAdd(name, (value, stored))) throw new ArgumentException("Duplicate setting.", nameof(name));
        descriptions.Add(name, description);
    }
    private T Get<T>(string name)
    {
        Demand("settings");
        if (!values.TryGetValue(name, out var item) || item.Value is not T value) throw new ArgumentException("Unknown setting or type mismatch.");
        return value;
    }
    private IAsyncAction Set<T>(string name, T value) where T : notnull
    {
        _ = Get<T>(name); Active();
        return Track(PersistAsync(name, value, reset: false)).AsAsyncAction();
    }
    private async Task PersistAsync(string name, object value, bool reset)
    {
        await writeGate.WaitAsync();
        try
        {
            Active();
            await Task.Run(() => SettingStore.Write(identity.SettingsDirectory, name, value, reset));
            // A durable write already completed; update cache even if closing.
            values[name] = (values[name].Default, value);
            if (phase == Phase.Active)
            {
                foreach (var observer in observers.ToArray())
                {
                    try { observer.OnChanged(name); }
                    catch (Exception error) { log(NotificationKind.Error, error.ToString()); }
                }
            }
        }
        finally { writeGate.Release(); }
    }
    public void RegisterInt32(string name, int defaultValue, string description) => Register(name, defaultValue, description);
    public void RegisterBoolean(string name, bool defaultValue, string description) => Register(name, defaultValue, description);
    public void RegisterString(string name, string defaultValue, string description) => Register(name, defaultValue, description);
    public int GetInt32(string name) => Get<int>(name);
    public bool GetBoolean(string name) => Get<bool>(name);
    public string GetString(string name) => Get<string>(name);
    public IAsyncAction SetInt32Async(string name, int newValue) => Set(name, newValue);
    public IAsyncAction SetBooleanAsync(string name, bool newValue) => Set(name, newValue);
    public IAsyncAction SetStringAsync(string name, string newValue) => Set(name, newValue);
    public IAsyncAction ResetAsync(string name)
    {
        Demand("settings"); Active();
        if (!values.TryGetValue(name, out var item)) throw new KeyNotFoundException(name);
        return Track(PersistAsync(name, item.Default, reset: true)).AsAsyncAction();
    }
    public IPluginRegistration Subscribe(IPluginSettingObserver observer)
    {
        Demand("settings");
        if (phase is Phase.Closing or Phase.Closed) throw new InvalidOperationException("Session closing.");
        if (observers.Count >= 32) throw new InvalidOperationException("Too many setting observers.");
        observers.Add(observer);
        return RegisterToken(() => observers.Remove(observer));
    }
}

public sealed record NavigationContribution(string Id, string Title, string IconGlyph, string ViewId);
internal sealed record CommandContribution(string Id, string Title);
internal sealed record RegisteredPluginSetting(string Name, string Description, object DefaultValue, object Value);

internal sealed partial class Registration(Action revoke) : IPluginRegistration
{
    private Action? callback = revoke;
    public void Revoke()
    {
        // Do not consume the token if thread validation or removal fails.
        Action? current = callback;
        if (current is null) return;
        current();
        callback = null;
    }
}
