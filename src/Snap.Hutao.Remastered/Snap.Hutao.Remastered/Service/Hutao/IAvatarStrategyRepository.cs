// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Abstraction;

namespace Snap.Hutao.Remastered.Service.Hutao;

internal interface IAvatarStrategyRepository : IRepository<AvatarStrategy>
{
    AvatarStrategy? GetStrategyByAvatarId(AvatarId avatarId);

    void AddStrategy(AvatarStrategy strategy);

    void RemoveStrategy(AvatarStrategy strategy);
}