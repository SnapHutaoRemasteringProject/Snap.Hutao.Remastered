// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Database.Abstraction;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Core.Database;

internal static class SelectableExtension
{
    extension<TSource>(IEnumerable<TSource> source)
        where TSource : ISelectable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TSource? SelectedOrFirstOrDefault()
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    return default;
                }

                TSource first = e.Current;

                do
                {
                    TSource result = e.Current;
                    if (!result.IsSelected)
                    {
                        continue;
                    }

                    while (e.MoveNext())
                    {
                        if (e.Current.IsSelected)
                        {
                            return default;
                        }
                    }

                    return result;
                }
                while (e.MoveNext());

                return first;
            }
        }
    }
}