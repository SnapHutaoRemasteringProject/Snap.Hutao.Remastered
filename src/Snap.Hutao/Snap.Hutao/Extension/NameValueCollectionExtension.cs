// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Specialized;

namespace Snap.Hutao.Extension;

internal static class NameValueCollectionExtension
{
    extension(NameValueCollection collection)
    {
        public bool TryGetSingleValue(string name, [NotNullWhen(true)] out string? value)
        {
            if (collection.GetValues(name) is [{ } single])
            {
                value = single;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}