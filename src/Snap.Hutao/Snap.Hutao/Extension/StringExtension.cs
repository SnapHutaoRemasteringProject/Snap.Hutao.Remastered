// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Snap.Hutao.Extension;

internal static class StringExtension
{
    extension(string value)
    {
        public bool EqualsAny(ReadOnlySpan<string> values, StringComparison stringComparison)
        {
            foreach (ref readonly string item in values)
            {
                if (value.Equals(item, stringComparison))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Uri ToUri()
        {
            return new(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string TrimEnd(string value1)
        {
            return value.AsSpan().TrimEnd(value1).ToString();
        }
    }
}