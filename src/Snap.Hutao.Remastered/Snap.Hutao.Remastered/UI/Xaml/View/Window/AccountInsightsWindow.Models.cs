// Copyright (c) Snap Hutao RP. All rights reserved.
using Snap.Hutao.Remastered.Model.Entity;
using System.Collections.Generic;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AccountInsightsWindow
{
    private enum InsightDirection
    {
        Neutral,
        HigherIsBetter,
        LowerIsBetter,
    }

    private sealed record InsightLoadResult(List<InsightSeries> Series, List<string> FailedSources, List<GachaItem> GachaItems, List<BeyondGachaItem> BeyondItems);

    private sealed record InsightPoint(string Period, double Value);

    private sealed record InsightSeries(
        string Title,
        string Metric,
        string Unit,
        List<InsightPoint> Points,
        InsightDirection Direction,
        bool IsTimeSeries = true);
}
