// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database.Abstraction;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Core.Database;

public static class AdvancedDbCollectionViewExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAdvancedDbCollectionView<TEntity> ToAdvancedDbCollectionView<[DynamicallyAccessedMembers(DatabaseEntityContract.Members)] TEntity>(this IList<TEntity> source, IServiceProvider serviceProvider)
        where TEntity : class, IPropertyValuesProvider, ISelectable
    {
        return new AdvancedDbCollectionView<TEntity>(source, serviceProvider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAdvancedDbCollectionView<TEntityAccess> ToAdvancedDbCollectionView<TEntityAccess, [DynamicallyAccessedMembers(DatabaseEntityContract.Members)] TEntity>(this IList<TEntityAccess> source, IServiceProvider serviceProvider)
        where TEntityAccess : class, IEntityAccess<TEntity>, IPropertyValuesProvider
        where TEntity : class, ISelectable
    {
        return new AdvancedDbCollectionView<TEntityAccess, TEntity>(source, serviceProvider);
    }
}
