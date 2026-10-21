// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.AI.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace Snap.Hutao.Remastered.Resource.Localization.AI;

internal sealed class StringResourceValue : ObservableObject
{
    private static readonly List<WeakReference<StringResourceValue>> registeredValues = [];

    static StringResourceValue()
    {
        StringResourceProxy.Default.PropertyChanged += OnStringResourceChanged;
    }

    private StringResourceValue()
    {
        lock (registeredValues)
        {
            registeredValues.Add(new(this));
        }
    }

    public SHName? Name { get; init; }

    public object?[]? Arguments { get; init; }

    public string Text { get; init; } = string.Empty;

    public string Value
    {
        get
        {
            if (Name is not SHName name)
            {
                return Text;
            }

            string value = StringResourceProxy.Default[name];
            if (Arguments is { Length: > 0 } arguments)
            {
                value = string.Format(StringResourceProxy.Default.CurrentCulture, value, Array.ConvertAll(arguments, NormalizeArgument));
            }

            return value;
        }
    }

    public static implicit operator StringResourceValue(SHName name)
    {
        return FromName(name);
    }

    public static implicit operator StringResourceValue(string? text)
    {
        return FromText(text);
    }

    public static StringResourceValue FromName(SHName name)
    {
        return new()
        {
            Name = name,
        };
    }

    public static StringResourceValue FromName(SHName name, params object?[]? arguments)
    {
        return new()
        {
            Name = name,
            Arguments = arguments,
        };
    }

    public static StringResourceValue FromText(string? text)
    {
        return new()
        {
            Text = text ?? string.Empty,
        };
    }

    private static void OnStringResourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!e.NameEquals(PropertyChangedEventArgs.Indexer))
        {
            return;
        }

        List<StringResourceValue> values = [];
        lock (registeredValues)
        {
            for (int i = registeredValues.Count - 1; i >= 0; i--)
            {
                if (registeredValues[i].TryGetTarget(out StringResourceValue? value))
                {
                    values.Add(value);
                }
                else
                {
                    registeredValues.RemoveAt(i);
                }
            }
        }

        foreach (StringResourceValue value in values)
        {
            value.OnPropertyChanged(nameof(Value));
        }
    }

    private static object? NormalizeArgument(object? argument)
    {
        return argument switch
        {
            StringResourceValue value => value.Value,
            SHName name => StringResourceProxy.Default[name],
            _ => argument,
        };
    }
}
