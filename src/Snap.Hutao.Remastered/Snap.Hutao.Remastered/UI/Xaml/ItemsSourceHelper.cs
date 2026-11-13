// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.Collections;

namespace Snap.Hutao.Remastered.UI.Xaml;

public static class ItemsSourceHelper
{
    // Invoke from x:Bind before the value reaches a WinRT object parameter.
    // A value converter would receive the already-boxed collection across ABI.
    // The flag also handles default ImmutableArray<T>, which cannot be enumerated.
    public static List<object?>? Create(IEnumerable? source, bool isDefaultOrEmpty)
    {
        if (source is null)
        {
            return null;
        }

        List<object?> items = [];
        if (!isDefaultOrEmpty)
        {
            foreach (object? item in source)
            {
                items.Add(item);
            }
        }

        return items;
    }
}
