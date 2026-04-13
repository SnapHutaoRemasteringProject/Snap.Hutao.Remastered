// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

[ExtendedEnum]
public enum ActStatus
{
    [LocalizationKey(nameof(SH.WebHoyolabGameRecordActCalendarActStatusUpcoming))]
    Upcoming = 1,

    [LocalizationKey(nameof(SH.WebHoyolabGameRecordActCalendarActStatusOngoing))]
    Ongoing = 2,
}