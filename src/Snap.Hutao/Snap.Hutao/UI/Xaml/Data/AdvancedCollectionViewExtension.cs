// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Snap.Hutao.UI.Xaml.Data;

internal static class AdvancedCollectionViewExtension
{
    extension<T>(IEnumerable<T> source)
        where T : class, IPropertyValuesProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAdvancedCollectionView<T> AsAdvancedCollectionView()
        {
            return source switch
            {
                IAdvancedCollectionView<T> advancedCollectionView => advancedCollectionView,
                IList<T> list => new AdvancedCollectionView<T>(list),
                _ => new AdvancedCollectionView<T>([.. source]),
            };
        }
    }
}