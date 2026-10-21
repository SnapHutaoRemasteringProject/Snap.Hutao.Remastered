// Copyright (c) Snap Hutao RP. All rights reserved.
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Charts;
using ChartSamples = Microsoft.UI.Xaml.Controls.Charts.Samples;
using WinUIChart = Microsoft.UI.Xaml.Controls.Charts.Chart;
using System;
using System.Globalization;
using System.Linq;

using Snap.Hutao.Remastered.Service.AccountInsights;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AccountInsightsWindow
{
    private UIElement CreateChart(InsightSeries series)
    {
        StackPanel panel = new() { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = series.Title + " · " + series.Metric
                + (series.Direction == InsightDirection.LowerIsBetter ? " (" + L("LowerIsBetter") + ")" : ""),
            FontSize = 18,
        });

        if (series.Points.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = L("NoData") });
            return panel;
        }

        double mean = series.Points.Average(point => point.Value);
        InsightPoint newest = series.Points[^1];
        CultureInfo culture = cultureOptions.CurrentCulture.Value;
        string summary;
        if (series.IsTimeSeries)
        {
            summary = string.Format(culture, L("SummaryTime"), series.Points.Count,
                mean.ToString("0.##", culture), newest.Value.ToString("0.##", culture), series.Unit);
            if (series.Points.Count > 1)
            {
                string change = (newest.Value - series.Points[^2].Value).ToString("+0.##;-0.##;0", culture);
                summary += string.Format(culture, L("SummaryDelta"), change, series.Unit);
            }
        }
        else
        {
            summary = string.Format(culture, L("SummaryPool"), series.Points.Count,
                series.Points.Sum(point => point.Value).ToString("0", culture), series.Unit);
        }

        panel.Children.Add(new TextBlock { Text = summary, TextWrapping = TextWrapping.Wrap });

        // Follow the WinUI Gallery's native Chart API. XamlChartsResources must be
        // registered at application scope, immediately after XamlControlsResources.
        ChartSamples categories = new()
        {
            ItemsSource = series.Points.Select(point => point.Period).ToArray(),
        };
        ChartSamples values = new()
        {
            ItemsSource = series.Points.Select(point => point.Value).ToArray(),
        };
        CategoryAxis categoryAxis = new()
        {
            Label = series.IsTimeSeries ? L("PeriodAxis") : series.Title,
            SortKey = CategorySortKey.Index,
            SortOrder = SortOrder.Ascending,
        };
        LinearAxis valueAxis = new()
        {
            Label = series.Metric + " (" + series.Unit + ")",
            Minimum = 0,
            GridLines = GridLines.Major,
        };

        WinUIChart chart = new()
        {
            Height = 320,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ShowLegend = false,
        };
        chart.Data.Add(categories);
        chart.Data.Add(values);
        chart.Axes.Add(categoryAxis);
        chart.Axes.Add(valueAxis);

        if (series.IsTimeSeries)
        {
            chart.Series.Add(new LineSeries
            {
                Title = series.Metric,
                ShowDataMarkers = true,
                ShowDataLabels = true,
                StrokeThickness = 2,
                XAxis = categoryAxis,
                XValues = categories,
                YAxis = valueAxis,
                YValues = values,
            });
        }
        else
        {
            chart.Series.Add(new BarSeries
            {
                Title = series.Metric,
                Orientation = BarOrientation.Vertical,
                ShowDataLabels = true,
                XAxis = categoryAxis,
                XValues = categories,
                YAxis = valueAxis,
                YValues = values,
            });
        }

        panel.Children.Add(chart);
        return panel;
    }


}
