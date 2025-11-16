// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Endpoint.Hutao;

internal interface IHomaSpiralAbyssEndpoints : IHomaRootAccess
{
    string RecordCheck(string uid)
    {
        return $"{Root}/Record/Check?Uid={uid}";
    }

    string RecordRank(string uid)
    {
        return $"{Root}/Record/Rank?Uid={uid}";
    }

    string RecordUpload()
    {
        return $"{Root}/Record/Upload";
    }

    string StatisticsOverview(bool last = false)
    {
        return $"{Root}/Statistics/Overview?Last={last}";
    }

    string StatisticsAvatarAttendanceRate(bool last = false)
    {
        return $"{Root}/Statistics/Avatar/AttendanceRate?Last={last}";
    }

    string StatisticsAvatarUtilizationRate(bool last = false)
    {
        return $"{Root}/Statistics/Avatar/UtilizationRate?Last={last}";
    }

    string StatisticsAvatarAvatarCollocation(bool last = false)
    {
        return $"{Root}/Statistics/Avatar/AvatarCollocation?Last={last}";
    }

    string StatisticsAvatarHoldingRate(bool last = false)
    {
        return $"{Root}/Statistics/Avatar/HoldingRate?Last={last}";
    }

    string StatisticsWeaponWeaponCollocation(bool last = false)
    {
        return $"{Root}/Statistics/Weapon/WeaponCollocation?Last={last}";
    }

    string StatisticsTeamCombination(bool last = false)
    {
        return $"{Root}/Statistics/Team/Combination?Last={last}";
    }
}