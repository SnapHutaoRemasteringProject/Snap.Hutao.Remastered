// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

public static class GachaEventSchedule
{
    // 换卡池当天算第 0 天，其后 7 天仍全开，含当天共 8 个自然日。
    private const int AllMaterialsOpenWindowLengthInDays = 8;

    /// <summary>
    /// 判断指定服务器本地日期是否处于某个"角色卡池开启后素材副本全天全开"窗口内。
    /// 触发卡池：角色活动祈愿(301) 与 特殊角色活动祈愿(400)，其 From 即版本更新/换卡池起点。
    /// 窗口 = 开启当天(第 0 天) + 其后 7 天。
    /// </summary>
    public static bool IsDateInAllMaterialsOpenWindow(this ImmutableArray<GachaEvent> events, in DateOnly date, in TimeSpan serverTimeZoneOffset)
    {
        foreach (GachaEvent gachaEvent in events)
        {
            if (gachaEvent.Type is not (GachaType.ActivityAvatar or GachaType.SpecialActivityAvatar))
            {
                continue;
            }

            DateOnly openStartDate = DateOnly.FromDateTime(gachaEvent.From.ToOffset(serverTimeZoneOffset).DateTime);
            DateOnly openEndDateExclusive = openStartDate.AddDays(AllMaterialsOpenWindowLengthInDays);
            if (date >= openStartDate && date < openEndDateExclusive)
            {
                return true;
            }
        }

        return false;
    }
}
