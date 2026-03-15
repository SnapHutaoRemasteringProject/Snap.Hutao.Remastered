using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.API.Setting;

public class PluginSettingRegistration<T>
{
    public string Name { get; }
    public string Description { get; }
    public T? DefaultValue { get; }
    public Type ValueType { get; }

    public PluginSettingRegistration(string name, string description, T? defaultValue = default)
    {
        if (!IsTypeSupported(typeof(T)))
        {
            throw new ArgumentException($"Type {typeof(T)} is not supported for plugin settings.");
        }

        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        DefaultValue = defaultValue;
        ValueType = typeof(T);
    }

    private static bool IsTypeSupported(Type type)
    {
        return SupportedTypes.Contains(type);
    }

    public static ImmutableHashSet<Type> SupportedTypes { get; } = ImmutableHashSet.Create<Type>(
        typeof(byte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(bool),
        typeof(char),
        typeof(string),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    );
}

public class PluginSettingInfo
{
    public string PluginId { get; }
    public string Name { get; }
    public string Description { get; }
    public Type ValueType { get; }
    public object? DefaultValue { get; }
    public object? CurrentValue { get; }

    public PluginSettingInfo(string pluginId, string name, string description, Type valueType, object? defaultValue, object? currentValue)
    {
        PluginId = pluginId;
        Name = name;
        Description = description;
        ValueType = valueType;
        DefaultValue = defaultValue;
        CurrentValue = currentValue;
    }
}
