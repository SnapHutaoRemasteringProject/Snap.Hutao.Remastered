// Copyright (c) Snap Hutao RP. All rights reserved.
using Microsoft.Extensions.Logging;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AccountInsightsWindow
{
    private static string FormatScheduleDate(Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DateTime time)
    {
        return $"{time.Year:D4}-{time.Month:D2}-{time.Day:D2}";
    }

    // A bad saved season must not prevent the other game modes from loading.
    private List<T> ReadSafely<T>(string source, Func<List<T>> read, List<string> failures)
    {
        try
        {
            return read();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Account insights could not load {Source}", source);
            failures.Add(source);
            return [];
        }
    }

    private InsightLoadResult CreateSeries(string uid, CultureInfo culture)
    {
        List<string> failures = [];

        List<GachaItem> gachaItems = ReadSafely("ChartWish", () =>
            gachaRepository.GetGachaArchiveByUid(uid) is { } archive
                ? gachaRepository.GetGachaItemImmutableArrayByArchiveId(archive.InnerId).ToList()
                : [], failures);

        List<InsightPoint> monthlyWishes = ReadSafely("ChartWish", () => gachaItems
            .GroupBy(item => item.Time.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new InsightPoint(group.Key, group.Count()))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightPoint> poolWishes = ReadSafely("ChartWish", () => gachaItems
            .GroupBy(item => item.QueryType)
            .OrderBy(group => (int)group.Key)
            .Select(group => new InsightPoint(GetWishPoolLabel(group.Key), group.Count()))
            .ToList(), failures);

        List<BeyondGachaItem> beyondItems = ReadSafely("ChartBeyond", () =>
            gachaRepository.GetGachaArchiveByUid(uid) is { } beyondArchive
                ? gachaRepository.GetBeyondGachaItemImmutableArrayByArchiveId(beyondArchive.InnerId).ToList()
                : [], failures);

        List<InsightPoint> monthlyBeyondWishes = ReadSafely("ChartBeyond", () => beyondItems
            .GroupBy(item => item.Time.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new InsightPoint(group.Key, group.Count()))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightPoint> beyondPoolWishes = ReadSafely("ChartBeyond", () => beyondItems
            .GroupBy(item => item.QueryType)
            .OrderBy(group => (int)group.Key)
            .Select(group => new InsightPoint(GetWishPoolLabel(group.Key), group.Count()))
            .ToList(), failures);

        List<InsightPoint> abyss = ReadSafely("ChartAbyss", () => abyssRepository.GetSpiralAbyssEntryMapByUid(uid).Values
            .Where(entry => entry.SpiralAbyss is { IsUnlock: true })
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                entry.SpiralAbyss.StartTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(entry.SpiralAbyss.StartTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : entry.ScheduleId.ToString(CultureInfo.InvariantCulture),
                entry.SpiralAbyss.TotalStar))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightPoint> theater = ReadSafely("ChartTheater", () => combatRepository.GetRoleCombatEntryMapByUid(uid).Values
            .Where(entry => entry.RoleCombatData is { HasData: true, Schedule: not null, Stat: not null })
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                FormatScheduleDate(entry.RoleCombatData.Schedule.StartDateTime),
                entry.RoleCombatData.Stat.MedalNumber))
            .TakeLast(12)
            .ToList(), failures);

        // Solo and multiplayer have different conditions; avoid mixing their records.
        List<InsightPoint> challenge = ReadSafely("ChartChallenge", () => challengeRepository.GetHardChallengeMapByUid(uid).Values
            .Where(entry => entry.HardChallengeData?.Schedule is not null
                && entry.HardChallengeData.SinglePlayer is { HasData: true, Best: not null })
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                FormatScheduleDate(entry.HardChallengeData.Schedule.StartDateTime),
                entry.HardChallengeData.SinglePlayer.Best!.Seconds))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightSeries> series =
        [
            new(Translate("ChartWish", culture), Translate("MetricMonthly", culture), Translate("UnitWish", culture), monthlyWishes, InsightDirection.Neutral),
            new(Translate("ChartWish", culture), Translate("MetricPool", culture), Translate("UnitWish", culture), poolWishes, InsightDirection.Neutral, false),
            new(Translate("ChartBeyond", culture), Translate("MetricMonthly", culture), Translate("UnitWish", culture), monthlyBeyondWishes, InsightDirection.Neutral),
            new(Translate("ChartBeyond", culture), Translate("MetricPool", culture), Translate("UnitWish", culture), beyondPoolWishes, InsightDirection.Neutral, false),
            new(Translate("ChartAbyss", culture), Translate("MetricAbyss", culture), Translate("UnitStar", culture), abyss, InsightDirection.HigherIsBetter),
            new(Translate("ChartTheater", culture), Translate("MetricTheater", culture), Translate("UnitMedal", culture), theater, InsightDirection.HigherIsBetter),
            new(Translate("ChartChallenge", culture), Translate("MetricChallenge", culture), Translate("UnitSeconds", culture), challenge, InsightDirection.LowerIsBetter),
        ];
        return new InsightLoadResult(series, failures.Distinct().ToList(), gachaItems, beyondItems);
    }

    private static string GetWishPoolLabel(GachaType type)
    {
        return type switch
        {
            GachaType.NewBie => SH.WebGachaConfigTypeNoviceWish,
            GachaType.Standard => SH.WebGachaConfigTypePermanentWish,
            GachaType.ActivityAvatar => SH.WebGachaConfigTypeAvatarEventWish,
            GachaType.ActivityWeapon => SH.WebGachaConfigTypeWeaponEventWish,
            GachaType.SpecialActivityAvatar => SH.WebGachaConfigTypeAvatarEventWish2,
            GachaType.ActivityCity => SH.WebGachaConfigTypeChronicledWish,
            GachaType.UGCStandard => SH.WebUGCGachaConfigTypePermanentWish,
            GachaType.UGCAvatarEventWish => SH.WebUGCGachaConfigTypeAvatarEventWish,
            GachaType.UGCActivityAvatarMaleOne => SH.WebUGCGachaConfigTypeActivityAvatarMaleOne,
            GachaType.UGCActivityAvatarMaleTwo => SH.WebUGCGachaConfigTypeActivityAvatarMaleTwo,
            GachaType.UGCActivityAvatarFemaleOne => SH.WebUGCGachaConfigTypeActivityAvatarFemaleOne,
            GachaType.UGCActivityAvatarFemaleTwo => SH.WebUGCGachaConfigTypeActivityAvatarFemaleTwo,
            _ => type.ToString(),
        };
    }


}
