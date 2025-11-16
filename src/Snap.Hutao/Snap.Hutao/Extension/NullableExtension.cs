// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Extension;

internal static class NullableExtension
{
    extension<T>(in T? nullable)
        where T : struct
    {
        public bool TryGetValue(out T value)
        {
            if (nullable.HasValue)
            {
                value = nullable.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}