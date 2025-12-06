// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Service.Hutao;

internal interface IAvatarStrategyService
{
    ValueTask<AvatarStrategy?> GetStrategyByAvatarId(AvatarId avatarId);
}