// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSortBox;

public sealed class AutoSortData<T>
{
    private readonly Func<T, T, AutoSortTokenKind, int> compareByKind;

    public AutoSortData(ImmutableArray<AutoSortToken> availableTokens, Func<T, T, AutoSortTokenKind, int> compareByKind)
    {
        AvailableTokens = availableTokens;
        this.compareByKind = compareByKind;
    }

    public ImmutableArray<AutoSortToken> AvailableTokens { get; }

    public IComparer<T>? Compile()
    {
        ImmutableArray<AutoSortToken> selected = [.. AvailableTokens.Where(t => t.IsSelected).OrderBy(t => t.SortOrder)];
        return selected.IsDefaultOrEmpty ? null : new SortComparer(selected, compareByKind);
    }

    private sealed class SortComparer(ImmutableArray<AutoSortToken> tokens, Func<T, T, AutoSortTokenKind, int> compareByKind) : IComparer<T>
    {
        public int Compare(T? x, T? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            foreach (AutoSortToken token in tokens)
            {
                int cmp = compareByKind(x, y, token.Kind);
                if (cmp != 0)
                {
                    return token.IsDescending ? -cmp : cmp;
                }
            }

            return 0;
        }
    }
}
