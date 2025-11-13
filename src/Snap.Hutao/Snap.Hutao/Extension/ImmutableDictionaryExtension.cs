// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace Snap.Hutao.Extension;

internal static class ImmutableDictionaryExtension
{
    extension<TKey, TSource>(IEnumerable<TSource> source)
        where TKey : notnull
    {
        [Pure]
        public ImmutableDictionary<TKey, TSource> ToImmutableDictionaryIgnoringDuplicateKeys(Func<TSource, TKey> keySelector)
        {
            ImmutableDictionary<TKey, TSource>.Builder builder = ImmutableDictionary.CreateBuilder<TKey, TSource>();

            foreach (TSource value in source)
            {
                builder[keySelector(value)] = value;
            }

            return builder.ToImmutable();
        }

        [Pure]
        public ImmutableDictionary<TKey, TValue> ToImmutableDictionaryIgnoringDuplicateKeys<TValue>(Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            ImmutableDictionary<TKey, TValue>.Builder builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();

            foreach (TSource value in source)
            {
                builder[keySelector(value)] = valueSelector(value);
            }

            return builder.ToImmutable();
        }
    }
}