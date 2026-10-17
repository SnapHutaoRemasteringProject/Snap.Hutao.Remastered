// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.


namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public readonly record struct LiquidGlassParameterRange(string Key, double Minimum, double Maximum, double DefaultValue);

public sealed class LiquidGlassOverrideStore
{
    private readonly Dictionary<string, LiquidGlassParameterRange> ranges;
    private readonly Dictionary<string, double> values = [];

    public LiquidGlassOverrideStore(string serialized, IEnumerable<LiquidGlassParameterRange> parameterRanges)
    {
        ranges = parameterRanges.ToDictionary(static range => range.Key);
        try
        {
            Dictionary<string, double>? stored = JsonSerializer.Deserialize<Dictionary<string, double>>(
                serialized, LiquidGlassProfileJsonContext.Default.Options);
            if (stored is not null)
                foreach (KeyValuePair<string, double> item in stored) TrySet(item.Key, item.Value);
        }
        catch (JsonException)
        {
            // Corrupt values are not allowed to reach Composition.
        }
    }

    public double Get(string key) => values.GetValueOrDefault(key, ranges[key].DefaultValue);
    public bool TrySet(string key, double value)
    {
        if (!ranges.TryGetValue(key, out LiquidGlassParameterRange range) || !double.IsFinite(value)
            || value < range.Minimum || value > range.Maximum || Get(key) == value) return false;
        if (value == range.DefaultValue) values.Remove(key);
        else values[key] = value;
        return true;
    }

    public void Reset() => values.Clear();
    public string Serialize() => JsonSerializer.Serialize(values, LiquidGlassProfileJsonContext.Default.Options);
}

[JsonSerializable(typeof(Dictionary<string, double>))]
public sealed partial class LiquidGlassProfileJsonContext : JsonSerializerContext;
