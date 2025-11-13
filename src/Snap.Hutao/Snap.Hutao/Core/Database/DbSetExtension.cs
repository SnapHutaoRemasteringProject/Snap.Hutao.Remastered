// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Core.Database;

internal static class DbSetExtension
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
            int count = dbContext.SaveChanges();
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