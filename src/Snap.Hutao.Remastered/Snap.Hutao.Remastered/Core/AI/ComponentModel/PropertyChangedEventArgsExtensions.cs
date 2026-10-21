// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.ComponentModel;

namespace Snap.Hutao.Remastered.Core.AI.ComponentModel;

internal static class PropertyChangedEventArgsExtensions
{
    // The extension block can not directly contains fields, so we have to declare the field outside of the block.
    private static readonly PropertyChangedEventArgs currentItem = new("CurrentItem");
    private static readonly PropertyChangedEventArgs indexer = new("Item[]");

    extension(PropertyChangedEventArgs)
    {
        public static PropertyChangedEventArgs CurrentItem { get => currentItem; }

        public static PropertyChangedEventArgs Indexer { get => indexer; }
    }

    extension(PropertyChangedEventArgs args)
    {
        public bool NameEquals(PropertyChangedEventArgs other)
        {
            return string.Equals(args.PropertyName, other.PropertyName, StringComparison.Ordinal);
        }
    }
}
