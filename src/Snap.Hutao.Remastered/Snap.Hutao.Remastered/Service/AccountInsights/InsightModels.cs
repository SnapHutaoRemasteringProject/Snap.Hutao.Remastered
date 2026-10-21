// Copyright (c) Snap Hutao RP. All rights reserved.
using Snap.Hutao.Remastered.Model.Entity;
using System.Collections.Generic;

namespace Snap.Hutao.Remastered.Service.AccountInsights;

internal enum InsightDirection
{
    Neutral,
    HigherIsBetter,
    LowerIsBetter,
}

internal sealed record InsightLoadResult(List<InsightSeries> Series, List<string> FailedSources, List<GachaItem> GachaItems, List<BeyondGachaItem> BeyondItems);

internal sealed record InsightPoint(string Period, double Value);

internal sealed record InsightSeries(
    string Title,
    string Metric,
    string Unit,
    List<InsightPoint> Points,
    InsightDirection Direction,
    bool IsTimeSeries = true,
    int? Difficulty = null);
