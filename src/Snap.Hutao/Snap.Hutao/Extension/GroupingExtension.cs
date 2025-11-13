// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Extension;

internal static class GroupingExtension
{
    extension<TKey, TElement>(IGrouping<TKey, TElement> grouping)
    {
        public void Deconstruct(out TKey key, out IEnumerable<TElement> elements)
        {
            key = grouping.Key;
            elements = grouping;
        }
    }
}