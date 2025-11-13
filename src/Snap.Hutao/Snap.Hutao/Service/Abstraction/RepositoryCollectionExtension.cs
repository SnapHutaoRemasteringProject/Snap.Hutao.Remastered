// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Snap.Hutao.Service.Abstraction;

internal static class RepositoryCollectionExtension
{
    extension<TEntity>(IRepository<TEntity> repository)
        where TEntity : class
    {
        public ImmutableArray<TEntity> ImmutableArray()
        {
            return repository.Query(query => query.ToImmutableArray());
        }

        public ImmutableArray<TEntity> ImmutableArray(Expression<Func<TEntity, bool>> predicate)
        {
            return repository.ImmutableArray(query => query.Where(predicate));
        }

        public ImmutableArray<TResult> ImmutableArray<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query)
        {
            return repository.Query(query1 => query(query1).ToImmutableArray());
        }

        // ObservableCollection<TEntity> is always not readonly.
        public ObservableCollection<TEntity> ObservableCollection()
        {
            return repository.Query(query => query.ToObservableCollection());
        }

        public ObservableCollection<TEntity> ObservableCollection(Expression<Func<TEntity, bool>> predicate)
        {
            return repository.Query(query => query.Where(predicate).ToObservableCollection());
        }
    }
}