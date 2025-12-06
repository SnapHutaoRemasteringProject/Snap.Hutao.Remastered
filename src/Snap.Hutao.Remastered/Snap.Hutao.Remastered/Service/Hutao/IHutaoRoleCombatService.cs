// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao.RoleCombat;

namespace Snap.Hutao.Remastered.Service.Hutao;

internal interface IHutaoRoleCombatService
{
    ValueTask<RoleCombatStatisticsItem> GetRoleCombatStatisticsItemAsync();
}