// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Extension;

internal static class ListExtension
{
    extension(List<int> source)
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public double Average()
        {
            if (source.Count <= 0)
            {
                return 0;
            }

            long sum = 0;
            foreach (int item in source)
            {
                sum += item;
            }

            return (double)sum / source.Count;
        }
    }

    extension<TSource>(List<TSource> source)
    {
        [Pure]
        public List<TSource> GetRange(in Range range)
        {
            (int start, int length) = range.GetOffsetAndLength(source.Count);
            return source.GetRange(start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public List<TSource> SortBy<TKey>(Func<TSource, TKey> keySelector)
            where TKey : IComparable
        {
            source.Sort((left, right) => keySelector(left).CompareTo(keySelector(right)));
            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public List<TSource> SortByDescending<TKey>(Func<TSource, TKey> keySelector)
            where TKey : IComparable
        {
            source.Sort((left, right) => keySelector(right).CompareTo(keySelector(left)));
            return source;
        }
    }

    extension<T>(List<T> list)
        where T : class
    {
        [Pure]
        public T? BinarySearch<TItem>(TItem item, Func<TItem, T, int> compare)
        {
            return CollectionsMarshal.AsSpan(list).BinarySearch(item, compare);
        }
    }

    extension<T>(IList<T> collection)
    {
        public void RemoveLast()
        {
            collection.RemoveAt(collection.Count - 1);
        }
    }
}