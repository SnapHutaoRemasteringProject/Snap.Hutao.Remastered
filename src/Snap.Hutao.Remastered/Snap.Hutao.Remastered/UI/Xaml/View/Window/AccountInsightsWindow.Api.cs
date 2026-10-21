// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.ApplicationModel.DataTransfer;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AccountInsightsWindow
{
    private static string CreateAnonymousJson(IEnumerable<InsightSeries> series)
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

    private void OnCopyClicked(object sender, RoutedEventArgs args)
    {
        if (snapshotJson is null)
        {
            return;
        }

        DataPackage package = new();
        package.SetText(snapshotJson);
        Clipboard.SetContent(package);
        StatusText.Text = L("Copied");
    }

}
