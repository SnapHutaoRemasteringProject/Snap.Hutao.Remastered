using Snap.Hutao.Plugin.Contracts;
using Windows.Foundation;

namespace Snap.Hutao.Plugin.SDK;

public sealed class PluginSettings
{
    private readonly PluginContext context;
    private readonly IPluginSettings abi;

    internal PluginSettings(PluginContext context, IPluginSettings abi)
    {
        this.context = context;
        this.abi = abi;
    }

    public PluginSetting<int> Register(string name, int defaultValue, string description = "")
    {
        abi.RegisterInt32(name, defaultValue, description);
        return new(context, abi, name, abi.GetInt32, abi.SetInt32Async);
    }

    public PluginSetting<bool> Register(string name, bool defaultValue, string description = "")
    {
        abi.RegisterBoolean(name, defaultValue, description);
        return new(context, abi, name, abi.GetBoolean, abi.SetBooleanAsync);
    }

    public PluginSetting<string> Register(string name, string defaultValue, string description = "")
    {
        abi.RegisterString(name, defaultValue, description);
        return new(context, abi, name, abi.GetString, abi.SetStringAsync);
    }
}

public sealed class PluginSetting<T>
{
    private readonly PluginContext context;
    private readonly IPluginSettings abi;
    private readonly Func<string, T> get;
    private readonly Func<string, T, IAsyncAction> set;

    internal PluginSetting(PluginContext context, IPluginSettings abi, string name, Func<string, T> get, Func<string, T, IAsyncAction> set)
    {
        this.context = context;
        this.abi = abi;
        Name = name;
        this.get = get;
        this.set = set;
    }

    public string Name { get; }
    public T Value => get(Name);
    public async Task SetValueAsync(T value) => await set(Name, value);
    public async Task ResetAsync() => await abi.ResetAsync(Name);
    public IDisposable Subscribe(Action callback)
        => context.Track(abi.Subscribe(new SettingObserver(Name, callback)));
}

internal sealed partial class SettingObserver(string name, Action callback) : IPluginSettingObserver
{
    public void OnChanged(string changed)
    {
        if (string.Equals(name, changed, StringComparison.Ordinal)) UiCall.Run(callback);
    }
}
