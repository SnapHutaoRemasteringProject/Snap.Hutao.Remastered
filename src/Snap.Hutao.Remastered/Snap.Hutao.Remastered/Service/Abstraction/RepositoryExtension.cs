// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Model.Entity.Database;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Service.Abstraction;

public static class RepositoryExtension
{
    public static TResult WithDatabase<TResult>(this IAppInfrastructureService repository, Func<AppDbContext, TResult> operation)
    {
        using IServiceScope scope = repository.ServiceProvider.CreateScope();
        return operation(scope.GetAppDbContext());
    }

    extension<TEntity>(IRepository<TEntity> repository)
        where TEntity : class
    {
        public TResult Execute<TResult>(Func<DbSet<TEntity>, TResult> func, [CallerMemberName] string caller = "")
        {
            using SentrySpanScope span = SentrySpanScope.StartChild("db.repository", $"{typeof(TEntity).Name}.{caller}");
            span
                .SetData("db.entity", typeof(TEntity).FullName ?? typeof(TEntity).Name)
                .SetData("db.repository_operation", caller);

            using (IServiceScope scope = repository.ServiceProvider.CreateScope())
            {
                AppDbContext appDbContext = scope.GetAppDbContext();
                return func(appDbContext.Set<TEntity>());
            }
        }

        public TEntity[] ReadAll()
        {
            return repository.WithDatabase(AppDbQueries.All<TEntity>);
        }

        public int Add(TEntity entity)
        {
            return repository.Execute(dbSet => dbSet.AddAndSave(entity));
        }

        public int AddRange(IEnumerable<TEntity> entities)
        {
            return repository.Execute(dbSet => dbSet.AddRangeAndSave(entities));
        }

        public int Delete()
        {
            return repository.WithDatabase(AppDbQueries.DeleteAll<TEntity>);
        }

        public int Delete(TEntity entity)
        {
            return repository.Execute(dbSet => dbSet.RemoveAndSave(entity));
        }

        public int Update(TEntity entity)
        {
            return repository.Execute(dbSet => dbSet.UpdateAndSave(entity));
        }
    }
}