// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Extension;

internal static class EnumerableExtension
{
    public static IEnumerable<KeyValuePair<TKey, int>> CountByKey<TKey, TValue>(this IEnumerable<Dictionary<TKey, TValue>> source, Func<TValue, bool> predicate)
        where TKey : notnull
    {
        return source.SelectMany(map => map.Where(kv => predicate(kv.Value))).CountBy(kv => kv.Key);
    }

    extension<T>(IEnumerable<T> source)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObservableCollection<T> ToObservableCollection()
        {
            return new(source);
        }

        public string ToString(char separator)
        {
            return string.Join(separator, source);
        }
    }
}