// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.User;

[Service(ServiceLifetime.Singleton, typeof(IUserRepository))]
public sealed partial class UserRepository : IUserRepository
{
    [GeneratedConstructor]
    public partial UserRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public void DeleteUserById(Guid id)
    {
        this.DeleteByInnerId(id);
    }

    public ImmutableArray<Model.Entity.User> GetUserImmutableArray()
    {
        return this.ImmutableArray();
    }

    public void UpdateUser(Model.Entity.User user)
    {
        this.Update(user);
    }

    public void ClearUserSelection()
    {
        this.WithDatabase(AppDbQueries.ClearSelection<Model.Entity.User>);
    }
}
