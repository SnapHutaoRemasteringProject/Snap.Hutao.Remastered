// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Entity.Abstraction;
using System.Collections.Immutable;

namespace Snap.Hutao.Service.Abstraction;

internal static class RepositoryAppDbEntityExtension
{
    extension<TEntity>(IRepository<TEntity> repository)
        where TEntity : class, IAppDbEntity
    {
        public int DeleteByInnerId(TEntity entity)
        {
            return repository.DeleteByInnerId(entity.InnerId);
        }

        public int DeleteByInnerId(Guid innerId)
        {
            return repository.Delete(e => e.InnerId == innerId);
        }
    }

    extension<TEntity>(IRepository<TEntity> repository)
        where TEntity : class, IAppDbEntityHasArchive
    {
        public ImmutableArray<TEntity> ImmutableArrayByArchiveId(Guid archiveId)
        {
            return repository.Query(query => query.Where(e => e.ArchiveId == archiveId).ToImmutableArray());
        }
    }
}