// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Web.Hutao.GachaLog;

namespace Snap.Hutao.Web.Endpoint.Hutao;

internal interface IHomaGachaLogEndpoints : IHomaRootAccess
{
    string GachaLogEndIds(string uid)
    {
        return $"{Root}/GachaLog/EndIds?Uid={uid}";
    }

    string GachaLogRetrieve()
    {
        return $"{Root}/GachaLog/Retrieve";
    }

    string GachaLogUpload()
    {
        return $"{Root}/GachaLog/Upload";
    }

    string GachaLogEntries()
    {
        return $"{Root}/GachaLog/Entries";
    }

    string GachaLogDelete(string uid)
    {
        return $"{Root}/GachaLog/Delete?Uid={uid}";
    }

    string GachaLogStatisticsCurrentEvents()
    {
        return $"{Root}/GachaLog/Statistics/CurrentEventStatistics";
    }

    string GachaLogStatisticsDistribution(GachaDistributionType distributionType)
    {
        return $"{Root}/GachaLog/Statistics/Distribution/{distributionType}";
    }
}