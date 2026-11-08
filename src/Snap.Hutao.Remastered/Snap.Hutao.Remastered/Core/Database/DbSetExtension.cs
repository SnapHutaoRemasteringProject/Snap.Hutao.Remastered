// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Snap.Hutao.Remastered.Core.Diagnostics;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Core.Database;

public static class DbSetExtension
{
    extension<TEntity>(DbSet<TEntity> dbSet)
        where TEntity : class
    {
        public int AddAndSave(TEntity entity)
        {
            dbSet.Add(entity);
            return dbSet.SaveChangesAndClearChangeTracker();
        }

        public int AddRangeAndSave(IEnumerable<TEntity> entities)
        {
            dbSet.AddRange(entities);
            return dbSet.SaveChangesAndClearChangeTracker();
        }

        public int RemoveAndSave(TEntity entity)
        {
            dbSet.Remove(entity);
            return dbSet.SaveChangesAndClearChangeTracker();
        }

        public int UpdateAndSave(TEntity entity)
        {
            dbSet.Update(entity);
            return dbSet.SaveChangesAndClearChangeTracker();
        }

        public int UpdateRangeAndSave(IEnumerable<TEntity> entity)
        {
            dbSet.UpdateRange(entity);
            return dbSet.SaveChangesAndClearChangeTracker();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SaveChangesAndClearChangeTracker()
        {
            DbContext dbContext = dbSet.Context();
            using SentrySpanScope span = SentrySpanScope.StartChild("db.save", typeof(TEntity).Name);
            int count = dbContext.SaveChanges();
            span
                .SetData("db.entity", typeof(TEntity).FullName ?? typeof(TEntity).Name)
                .SetData("db.rows_affected", count);
            dbContext.ChangeTracker.Clear();
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DbContext Context()
        {
            return dbSet.GetService<ICurrentDbContext>().Context;
        }
    }
}