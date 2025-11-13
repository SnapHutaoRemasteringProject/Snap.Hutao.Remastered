// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Numerics;

namespace Snap.Hutao.Extension;

internal static class SpanExtension
{
    extension<T>(ReadOnlySpan<T> span)
        where T : IEquatable<T>
    {
        public ReadOnlySpan<T> After(T separator)
        {
            int indexOfSeparator = span.IndexOf(separator);
            return indexOfSeparator > 0 ? span[(indexOfSeparator + 1)..] : span;
        }

        public ReadOnlySpan<T> Before(T separator)
        {
            int indexOfSeparator = span.IndexOf(separator);
            return indexOfSeparator > 0 ? span[..indexOfSeparator] : span;
        }

        public bool TrySplitIntoTwo(T separator, out ReadOnlySpan<T> left, out ReadOnlySpan<T> right)
        {
            int indexOfSeparator = span.IndexOf(separator);

            if (indexOfSeparator > 0)
            {
                left = span[..indexOfSeparator];
                right = span[(indexOfSeparator + 1)..];

                return true;
            }

            left = default;
            right = default;
            return false;
        }
    }

    extension<TItem, T>(ReadOnlySpan<T> span)
        where T : class
    {
        [Pure]
        public T? BinarySearch(TItem item, Func<TItem, T, int> compare)
        {
            int left = 0;
            int right = span.Length - 1;

            while (left <= right)
            {
                int middle = (int)(((uint)left + (uint)right) >> 1);
                ref readonly T current = ref span[middle];
                switch (compare(item, current))
                {
                    case 0:
                        return current;
                    case < 0:
                        right = middle - 1;
                        break;
                    default:
                        left = middle + 1;
                        break;
                }
            }

            return default;
        }
    }

    extension<T>(ReadOnlySpan<T> span)
        where T : INumber<T>, IMinMaxValue<T>
    {
        public int IndexOfMax()
        {
            T max = T.MinValue;
            int maxIndex = 0;
            for (int i = 0; i < span.Length; i++)
            {
                ref readonly T current = ref span[i];
                if (current > max)
                {
                    maxIndex = i;
                    max = current;
                }
            }

            return maxIndex;
        }
    }

    extension(ReadOnlySpan<byte> span)
    {
        public byte Average()
        {
            if (span.IsEmpty)
            {
                return 0;
            }

            int sum = 0;
            int count = 0;
            foreach (ref readonly byte b in span)
            {
                sum += b;
                count++;
            }

            // ReSharper disable once IntDivisionByZero
            return unchecked((byte)(sum / count));
        }
    }
}