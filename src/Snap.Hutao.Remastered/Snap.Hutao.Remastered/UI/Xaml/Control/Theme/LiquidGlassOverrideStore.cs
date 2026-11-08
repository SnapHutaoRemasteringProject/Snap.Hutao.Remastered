// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public readonly record struct LiquidGlassParameterRange(string Key, double Minimum, double Maximum, double DefaultValue);

public sealed class LiquidGlassOverrideStore
{
    private readonly Dictionary<string, LiquidGlassParameterRange> ranges;
    private readonly Dictionary<string, double> values = [];
    private readonly Dictionary<string, double> committedValues = [];

    public long Revision { get; private set; }
    public bool HasPendingChanges => values.Count != committedValues.Count
        || values.Any(item => !committedValues.TryGetValue(item.Key, out double committed) || item.Value != committed);

    public LiquidGlassOverrideStore(string serialized, IEnumerable<LiquidGlassParameterRange> parameterRanges,
        Action<Exception, string, double?>? rejected = null)
    {
        ranges = parameterRanges.ToDictionary(static range => range.Key);
        try
        {
            Dictionary<string, double>? stored = JsonSerialize.Deserialize<Dictionary<string, double>>(
                serialized, LiquidGlassProfileJsonContext.Default.Options);
            if (stored is not null)
                foreach (KeyValuePair<string, double> item in stored)
                {
                    try
                    {
                        Stage(item.Key, item.Value);
                    }
                    catch (ArgumentException exception)
                    {
                        rejected?.Invoke(exception, item.Key, item.Value);
                    }
                }
        }
        catch (JsonException exception)
        {
            rejected?.Invoke(exception, string.Empty, null);
        }

        Commit();
    }

    public double Get(string key) => values.GetValueOrDefault(key, ranges[key].DefaultValue);

    public bool Stage(string key, double value)
    {
        if (!ranges.TryGetValue(key, out LiquidGlassParameterRange range))
            throw new ArgumentException("Unknown liquid glass parameter.", nameof(key));
        if (!double.IsFinite(value) || value < range.Minimum || value > range.Maximum)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                FormattableString.Invariant($"{key} must be finite and between {range.Minimum:R} and {range.Maximum:R}."));
        return TrySet(key, value);
    }
    public bool TrySet(string key, double value)
    {
        if (!ranges.TryGetValue(key, out LiquidGlassParameterRange range) || !double.IsFinite(value)
            || value < range.Minimum || value > range.Maximum || Get(key) == value) return false;
        if (value == range.DefaultValue) values.Remove(key);
        else values[key] = value;
        Revision++;
        return true;
    }

    public void Reset()
    {
        if (values.Count == 0) return;
        values.Clear();
        Revision++;
    }

    public void Commit()
    {
        committedValues.Clear();
        foreach (KeyValuePair<string, double> item in values) committedValues.Add(item.Key, item.Value);
    }

    public void Rollback()
    {
        if (!HasPendingChanges) return;
        values.Clear();
        foreach (KeyValuePair<string, double> item in committedValues) values.Add(item.Key, item.Value);
        Revision++;
    }

    public bool RestoreRejected(string key)
    {
        LiquidGlassParameterRange range = ranges[key];
        double value = Get(key);
        double committed = committedValues.GetValueOrDefault(key, range.DefaultValue);
        if (value != committed) return TrySet(key, committed);
        if (value == range.DefaultValue) return false;
        values.Remove(key);
        committedValues.Remove(key);
        Revision++;
        return true;
    }

    public string Serialize() => JsonSerialize.Serialize(values, LiquidGlassProfileJsonContext.Default.Options);
    public string SerializeCommitted() => JsonSerialize.Serialize(committedValues, LiquidGlassProfileJsonContext.Default.Options);
}

[JsonSerializable(typeof(Dictionary<string, double>))]
public sealed partial class LiquidGlassProfileJsonContext : JsonSerializerContext;
