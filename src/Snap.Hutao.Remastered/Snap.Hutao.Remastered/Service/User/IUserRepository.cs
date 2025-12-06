// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.User;

internal interface IUserRepository : IRepository<Model.Entity.User>
{
    void DeleteUserById(Guid id);

    ImmutableArray<Model.Entity.User> GetUserImmutableArray();

    void UpdateUser(Model.Entity.User user);

    void ClearUserSelection();
}