// Copyright (c) Snap Hutao RP. All rights reserved.
using Microsoft.Extensions.Logging;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Snap.Hutao.Remastered.Service.GachaLog;
using Snap.Hutao.Remastered.Service.SpiralAbyss;
using Snap.Hutao.Remastered.Service.RoleCombat;
using Snap.Hutao.Remastered.Service.HardChallenge;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Snap.Hutao.Remastered.Service.AccountInsights;

[Service(ServiceLifetime.Scoped)]
internal sealed class AccountInsightsHistoryService(IGachaLogRepository gachaRepository, ISpiralAbyssRepository abyssRepository,
    IRoleCombatRepository combatRepository, IHardChallengeRepository challengeRepository, ILogger<AccountInsightsHistoryService> logger)
{
    private static string Translate(string name, CultureInfo culture) => SH.ResourceManager.GetString("ViewDialogAccountInsights" + name, culture) ?? name;
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

    public InsightLoadResult CreateSeries(string uid, CultureInfo culture)
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

        // Keep different difficulty levels in separate series and snapshots.
        var challenge = ReadSafely("ChartChallenge", () => challengeRepository.GetHardChallengeMapByUid(uid).Values
            .Where(entry => entry.HardChallengeData?.Schedule is not null
                && entry.HardChallengeData.SinglePlayer is { HasData: true, Best: not null })
            .OrderBy(entry => entry.ScheduleId).ToList(), failures);

        List<InsightSeries> series =
        [
            new(Translate("ChartWish", culture), Translate("MetricMonthly", culture), Translate("UnitWish", culture), monthlyWishes, InsightDirection.Neutral),
            new(Translate("ChartWish", culture), Translate("MetricPool", culture), Translate("UnitWish", culture), poolWishes, InsightDirection.Neutral, false),
            new(Translate("ChartBeyond", culture), Translate("MetricMonthly", culture), Translate("UnitWish", culture), monthlyBeyondWishes, InsightDirection.Neutral),
            new(Translate("ChartBeyond", culture), Translate("MetricPool", culture), Translate("UnitWish", culture), beyondPoolWishes, InsightDirection.Neutral, false),
            new(Translate("ChartAbyss", culture), Translate("MetricAbyss", culture), Translate("UnitStar", culture), abyss, InsightDirection.HigherIsBetter),
            new(Translate("ChartTheater", culture), Translate("MetricTheater", culture), Translate("UnitMedal", culture), theater, InsightDirection.HigherIsBetter),
        ];
        foreach (var difficulty in challenge.GroupBy(entry => entry.HardChallengeData.SinglePlayer.Best!.Difficulty))
        {
            List<InsightPoint> points = difficulty.Select(entry => new InsightPoint(
                FormatScheduleDate(entry.HardChallengeData.Schedule.StartDateTime),
                entry.HardChallengeData.SinglePlayer.Best!.Seconds)).TakeLast(12).ToList();
            series.Add(new(Translate("ChartChallenge", culture),
                Translate("MetricChallenge", culture) + " (" + difficulty.Key + ")",
                Translate("UnitSeconds", culture), points, InsightDirection.LowerIsBetter, Difficulty: (int)difficulty.Key));
        }
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


    public static string CreateAnonymousJson(IEnumerable<InsightSeries> series)
    {
        JsonArray data = [];
        foreach (InsightSeries item in series)
        {
            JsonArray points = [];
            foreach (InsightPoint point in item.Points)
            {
                points.Add(new JsonObject { ["period"] = point.Period, ["value"] = point.Value });
            }

            double[] values = item.Points.Select(point => point.Value).ToArray();
            JsonObject summary = new()
            {
                ["recorded_period_count"] = values.Length,
                ["latest"] = item.IsTimeSeries && values.Length > 0 ? JsonValue.Create(values[^1]) : null,
                ["minimum"] = values.Length > 0 ? JsonValue.Create(values.Min()) : null,
                ["maximum"] = values.Length > 0 ? JsonValue.Create(values.Max()) : null,
                ["average"] = values.Length > 0 ? JsonValue.Create(Math.Round(values.Average(), 2)) : null,
                ["latest_change_from_previous"] = item.IsTimeSeries && values.Length > 1 ? JsonValue.Create(values[^1] - values[^2]) : null,
            };

            data.Add(new JsonObject
            {
                ["summary"] = summary,
                ["difficulty"] = item.Difficulty,
                ["kind"] = item.IsTimeSeries ? "time_series" : "category_distribution",
                ["category"] = item.Title,
                ["metric"] = item.Metric,
                ["unit"] = item.Unit,
                ["direction"] = item.Direction switch
                {
                    InsightDirection.HigherIsBetter => "higher_is_better",
                    InsightDirection.LowerIsBetter => "lower_is_better",
                    _ => "neutral",
                },
                ["periods"] = points,
            });
        }

        return new JsonObject
        {
            ["schema_version"] = 1,
            ["scope"] = "Saved local history only; the most recent twelve periods are included for time-series charts; missing periods are not zero. Direct account identifiers are excluded, but history patterns may remain linkable.",
            ["series"] = data,
        }.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

}
