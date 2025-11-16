// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;

namespace Snap.Hutao.Extension;

internal static class CollectionExtension
{
    extension<T>(Collection<T> collection)
    {
        public int RemoveWhere(Func<T, bool> shouldRemovePredicate)
        {
            int count = 0;
            T[] array = GC.AllocateUninitializedArray<T>(collection.Count);
            collection.CopyTo(array, 0);
            foreach (T item in array)
            {
                if (shouldRemovePredicate(item))
                {
                    collection.Remove(item);
                    count++;
                }
            }

            return count;
        }

        [Pure]
        public int FirstIndexOf(Func<T, bool> predicate)
        {
            for (int index = 0; index < collection.Count; index++)
            {
                if (predicate(collection[index]))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}