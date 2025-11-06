// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Endpoint.Hutao;

internal interface IHomaRoleCombatEndpoints : IHomaRootAccess
{
    string RoleCombatRecordUpload()
    {
        return $"{Root}/RoleCombat/Upload";
    }

    string RoleCombatStatistics(bool last = false)
    {
        return $"{Root}/RoleCombat/Statistics?Last={last}";
    }
}