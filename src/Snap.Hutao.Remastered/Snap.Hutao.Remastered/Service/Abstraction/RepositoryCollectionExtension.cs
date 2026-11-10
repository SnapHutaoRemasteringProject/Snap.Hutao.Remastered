// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database.Abstraction;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.Abstraction;

public static class RepositoryCollectionExtension
{
    extension<[DynamicallyAccessedMembers(DatabaseEntityContract.Members)] TEntity>(IRepository<TEntity> repository)
        where TEntity : class
    {
        public ImmutableArray<TEntity> ImmutableArray()
        {
            return repository.ReadAll().ToImmutableArray();
        }

        // ObservableCollection<TEntity> is always not readonly.
        public ObservableCollection<TEntity> ObservableCollection()
        {
            return new(repository.ReadAll());
        }

    }
}
