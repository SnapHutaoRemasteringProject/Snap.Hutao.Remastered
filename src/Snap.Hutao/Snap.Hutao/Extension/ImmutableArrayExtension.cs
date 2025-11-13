// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using RequireStaticDelegate = JetBrains.Annotations.RequireStaticDelegateAttribute;

namespace Snap.Hutao.Extension;

internal static class ImmutableArrayExtension
{
    extension<TElement>(ImmutableArray<TElement> array)
    {
        [Pure]
        public ImmutableArray<TElement> EmptyIfDefault()
        {
            return array.IsDefault ? [] : array;
        }

        [Pure]
        public ImmutableArray<TElement> Reverse()
        {
            if (array.IsEmpty)
            {
                return array;
            }

            TElement[] reversed = GC.AllocateUninitializedArray<TElement>(array.Length);
            array.AsSpan().CopyTo(reversed);
            Array.Reverse(reversed);
            return ImmutableCollectionsMarshal.AsImmutableArray(reversed);
        }

        public void ReverseInPlace()
        {
            if (array.IsEmpty)
            {
                return;
            }

            TElement[]? raw = ImmutableCollectionsMarshal.AsArray(array);
            ArgumentNullException.ThrowIfNull(raw);
            Array.Reverse(raw);
        }

        [Pure]
        public ImmutableArray<TResult> SelectAsArray<TResult>([RequireStaticDelegate] Func<TElement, TResult> selector)
        {
            return ImmutableArray.CreateRange(array, selector);
        }

        [Pure]
        public ImmutableArray<TResult> SelectAsArray<TResult, TState>([RequireStaticDelegate] Func<TElement, TState, TResult> selector, TState state)
        {
            return ImmutableArray.CreateRange(array, selector, state);
        }

        [Pure]
        public ImmutableArray<TResult> SelectAsArray<TResult>([RequireStaticDelegate] Func<TElement, int, TResult> selector)
        {
            int length = array.Length;
            if (length == 0)
            {
                return [];
            }

            TResult[] results = GC.AllocateUninitializedArray<TResult>(length);

            for (int index = 0; index < array.Length; index++)
            {
                results[index] = selector(array[index], index);
            }

            return ImmutableCollectionsMarshal.AsImmutableArray(results);
        }

        [Pure]
        public ImmutableArray<TResult> SelectAsArray<TState, TResult>([RequireStaticDelegate] Func<TElement, int, TState, TResult> selector, TState state)
        {
            int length = array.Length;
            if (length == 0)
            {
                return [];
            }

            TResult[] results = GC.AllocateUninitializedArray<TResult>(length);

            for (int index = 0; index < array.Length; index++)
            {
                results[index] = selector(array[index], index, state);
            }

            return ImmutableCollectionsMarshal.AsImmutableArray(results);
        }

        public void SortInPlace(IComparer<TElement> comparer)
        {
            if (array.IsEmpty)
            {
                return;
            }

            TElement[]? raw = ImmutableCollectionsMarshal.AsArray(array);
            ArgumentNullException.ThrowIfNull(raw);
            Array.Sort(raw, comparer);
        }
    }
}