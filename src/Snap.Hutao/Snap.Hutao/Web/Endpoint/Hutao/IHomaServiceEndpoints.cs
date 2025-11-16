// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Endpoint.Hutao;

internal interface IHomaServiceEndpoints : IHomaRootAccess
{
    string Announcement(string locale)
    {
        return $"{Root}/Announcement/List?locale={locale}";
    }

    string AnnouncementUpload()
    {
        return $"{Root}/Service/Announcement/Upload";
    }

    string GachaLogCompensation(int days)
    {
        return $"{Root}/Service/GachaLog/Compensation?days={days}";
    }

    string GachaLogDesignation(string userName, int days)
    {
        return $"{Root}/Service/GachaLog/Designation?userName={userName}&days={days}";
    }

    string CdnCompensation(int days)
    {
        return $"{Root}/Service/Distribution/Compensation?days={days}";
    }

    string CdnDesignation(string userName, int days)
    {
        return $"{Root}/Service/Distribution/Designation?userName={userName}&days={days}";
    }

    string RedeemCodeGenerate()
    {
        return $"{Root}/Service/Redeem/Generate";
    }
}