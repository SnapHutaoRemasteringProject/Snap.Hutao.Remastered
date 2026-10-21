// Copyright (c) Snap Hutao RP. All rights reserved.
using Microsoft.Extensions.Logging;
using Snap.Hutao.Remastered.Service.GachaLog;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.ViewModel.GachaLog;
using Snap.Hutao.Remastered.Model.Intrinsic;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Snap.Hutao.Remastered.Service.AccountInsights;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AccountInsightsWindow
{
    private async Task LoadHuTaoStatisticsAsync(InsightLoadResult result)
    {
        if (result.GachaItems.Count == 0 && result.BeyondItems.Count == 0)
        {
            return;
        }

        try
        {
            // Metadata is normally prepared by the app at startup. A bounded wait
            // avoids delaying the dashboard when the metadata repository is offline.
            bool initialized = await metadataService.InitializeAsync()
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(10), lifetime.Token);
            if (!initialized)
            {
                return;
            }

            GachaLogServiceMetadataContext metadata =
                await metadataService.GetContextAsync<GachaLogServiceMetadataContext>(lifetime.Token);

            GachaStatistics statistics = await statisticsFactory.CreateAsync(
                metadata, result.GachaItems.ToImmutableArray(), result.BeyondItems.ToImmutableArray());

            lifetime.Token.ThrowIfCancellationRequested();

            JsonObject package = JsonNode.Parse(snapshotJson!)!.AsObject();
            package["hu_tao_computed_gacha_statistics"] = BuildComputedGachaStatistics(statistics);
            snapshotJson = package.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            // Featured characters are metadata-derived; do not treat every 5-star as UP.
            List<InsightPoint> featuredCharacters = statistics.AvatarWish.SummaryList
                .Where(item => item.Quality == QualityType.QUALITY_ORANGE && item.IsUp)
                .GroupBy(item => item.Name)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.CurrentCulture)
                .Take(15)
                .Select(group => new InsightPoint(group.Key, group.Count()))
                .ToList();

            if (featuredCharacters.Count > 0)
            {
                InsightSeries featured = new(
                    L("ChartWish"), L("MetricFeaturedCharacters"), L("UnitWish"),
                    featuredCharacters, InsightDirection.Neutral, false);
                try
                {
                    ChartsPanel.Children.Add(CreateChart(featured));
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to render featured five-star chart");
                }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            // The window was closed.
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not load metadata-backed Gacha statistics for account insights");
            if (!lifetime.IsCancellationRequested)
            {
                StatusText.Text = L("Ready") + " " + L("GachaDetailsUnavailable");
            }
        }
    }

    private static JsonNode? Finite(double value)
    {
        return double.IsFinite(value) ? JsonValue.Create(value) : null;
    }

    private static JsonObject BuildComputedGachaStatistics(GachaStatistics statistics)
    {
        JsonArray pools = [];
        (string Name, TypedWishSummary Value)[] summaries =
        [
            ("avatar_event", statistics.AvatarWish),
            ("weapon_event", statistics.WeaponWish),
            ("chronicled", statistics.ChronicledWish),
            ("standard", statistics.StandardWish),
            ("novice", statistics.NoviceWish),
            ("beyond_standard", statistics.BeyondStandardWish),
            ("beyond_event", statistics.BeyondEventWish),
        ];

        foreach ((string name, TypedWishSummary summary) in summaries)
        {
            JsonArray recentGold = [];
            foreach (SummaryItem wish in summary.SummaryList
                .Where(item => item.Quality == QualityType.QUALITY_ORANGE)
                .OrderByDescending(item => item.Time)
                .Take(35))
            {
                recentGold.Add(new JsonObject
                {
                    ["name"] = wish.Name,
                    ["date"] = wish.Time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["pulls_for_this_five_star"] = wish.LastPull,
                    ["is_featured"] = wish.IsUp,
                    ["is_guaranteed"] = wish.IsGuarantee,
                    ["pulls_since_previous_featured"] = wish.IsUp ? JsonValue.Create(wish.TotalCyclePull) : null,
                });
            }

            pools.Add(new JsonObject
            {
                ["pool"] = name,
                ["total_wishes"] = summary.TotalCount,
                ["five_star_count"] = summary.TotalOrangePull,
                ["four_star_count"] = summary.TotalPurplePull,
                ["five_star_rate_in_observed_wishes"] = Finite(summary.TotalOrangePercent),
                ["average_wishes_per_five_star"] = Finite(summary.AverageOrangePull),
                ["average_wishes_per_featured_five_star"] = Finite(summary.AverageUpOrangePull),
                ["minimum_wishes_per_five_star"] = summary.MinOrangePull,
                ["maximum_wishes_per_five_star"] = summary.MaxOrangePull,
                ["current_wishes_since_last_five_star"] = summary.LastOrangePull,
                ["game_guarantee_threshold"] = summary.GuaranteeOrangeThreshold,
                ["recorded_five_star_obtains"] = recentGold,
            });
        }

        JsonArray featuredBanners = [];
        foreach (HistoryWish eventWish in statistics.HistoryWishes.View.Take(24))
        {
            JsonArray featuredNames = [];
            foreach (StatisticsItem featured in eventWish.OrangeUpList.Take(6))
            {
                featuredNames.Add(new JsonObject
                {
                    ["name"] = featured.Name,
                    ["recorded_obtain_count"] = featured.Count,
                });
            }

            featuredBanners.Add(new JsonObject
            {
                ["from"] = eventWish.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["to"] = eventWish.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["featured_five_stars"] = featuredNames,
            });
        }

        return new JsonObject
        {
            ["origin"] = "Snap Hutao's existing GachaStatisticsFactory; not re-inferred from monthly counts",
            ["scope_note"] = "Incomplete imported histories can distort observed rarity, pity and 50/50 results. Never claim exact luck percentiles.",
            ["pools"] = pools,
            ["recent_banner_featured_characters_and_weapons"] = featuredBanners,
        };
    }


}
