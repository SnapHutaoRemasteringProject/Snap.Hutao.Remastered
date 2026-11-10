// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity.Abstraction;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Database.Abstraction;

namespace Snap.Hutao.Remastered.Service.Abstraction;

public static class RepositoryAppDbEntityExtension
{
    extension<[DynamicallyAccessedMembers(DatabaseEntityContract.Members)] TEntity>(IRepository<TEntity> repository)
        where TEntity : class, IAppDbEntity
    {
        public int DeleteByInnerId(TEntity entity)
        {
            return repository.DeleteByInnerId(entity.InnerId);
        }

        public int DeleteByInnerId(Guid innerId)
        {
            return repository.WithDatabase(context => AppDbQueries.DeleteByInnerId<TEntity>(context, innerId));
        }
    }

}
