// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

internal interface IInfrastructureStrategyEndpoints : IInfrastructureRootAccess
{
    string StrategyAll()
    {
        return $"{Root}/strategy/all";
    }

    public string StrategyItem(AvatarId avatarId)
    {
        return $"{Root}/strategy/item?item_id={avatarId}";
    }
}