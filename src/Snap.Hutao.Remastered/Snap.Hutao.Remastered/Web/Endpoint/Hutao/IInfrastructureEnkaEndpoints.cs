// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab;

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

internal interface IInfrastructureEnkaEndpoints : IInfrastructureRootAccess
{
    string Enka(PlayerUid uid)
    {
        return $"{Root}/enka/{uid}";
    }

    string EnkaPlayerInfo(PlayerUid uid)
    {
        return $"{Root}/enka/{uid}/info";
    }
}