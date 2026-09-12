// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Ledger;
using Windows.Foundation;
using Windows.UI;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

/// <summary>
/// 将旅行者札记的月度原石来源转换为环形图扇区（同时提供图例所需的文本与颜色）。
/// </summary>
public sealed partial class LedgerIncomeSourceToDonutSliceConverter : ValueConverter<IReadOnlyList<LedgerIncomeSource>, IReadOnlyList<LedgerIncomeSlice>>
{
    private const double BoxSize = 104;
    private const double Center = BoxSize / 2;
    private const double OuterRadius = 50;
    private const double InnerRadius = 33;
    private const double StartAngle = -90;

    private static readonly Color[] Palette =
    [
        Color.FromArgb(0xFF, 0xFF, 0xB1, 0x3D),
        Color.FromArgb(0xFF, 0x4C, 0x9B, 0xE8),
        Color.FromArgb(0xFF, 0x6D, 0xC9, 0x6D),
        Color.FromArgb(0xFF, 0xB5, 0x7B, 0xFF),
        Color.FromArgb(0xFF, 0xFF, 0x8A, 0x8A),
        Color.FromArgb(0xFF, 0x35, 0xC4, 0xC4),
        Color.FromArgb(0xFF, 0xFF, 0x9E, 0xD2),
        Color.FromArgb(0xFF, 0x9A, 0xA5, 0xB1),
    ];

    public override IReadOnlyList<LedgerIncomeSlice> Convert(IReadOnlyList<LedgerIncomeSource> from)
    {
        List<LedgerIncomeSource> sources = [.. from.Where(source => source.Num > 0)];
        if (sources.Count is 0)
        {
            return [];
        }

        long total = 0;
        foreach (LedgerIncomeSource source in sources)
        {
            total += source.Num;
        }

        List<LedgerIncomeSlice> slices = new(sources.Count);
        double angle = StartAngle;
        for (int i = 0; i < sources.Count; i++)
        {
            LedgerIncomeSource source = sources[i];
            double sweepAngle = 360 * source.Num / total;

            slices.Add(new()
            {
                Action = source.Action,
                FormattedNum = source.Num.ToString("N0"),
                FormattedPercent = $"{source.Percent}%",
                Data = CreateRingSlice(angle, sweepAngle),
                Fill = new SolidColorBrush(Palette[i % Palette.Length]),
            });

            angle += sweepAngle;
        }

        return slices;
    }

    private static Geometry CreateRingSlice(double startAngle, double sweepAngle)
    {
        if (sweepAngle >= 359.995)
        {
            GeometryGroup ring = new() { FillRule = FillRule.EvenOdd };
            ring.Children.Add(new EllipseGeometry { Center = new(Center, Center), RadiusX = OuterRadius, RadiusY = OuterRadius });
            ring.Children.Add(new EllipseGeometry { Center = new(Center, Center), RadiusX = InnerRadius, RadiusY = InnerRadius });
            return ring;
        }

        bool isLargeArc = sweepAngle > 180;
        double endAngle = startAngle + sweepAngle;

        PathFigure figure = new()
        {
            StartPoint = PointOnArc(startAngle, OuterRadius),
            IsClosed = true,
        };

        figure.Segments.Add(new ArcSegment
        {
            Point = PointOnArc(endAngle, OuterRadius),
            Size = new(OuterRadius, OuterRadius),
            IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Clockwise,
        });

        figure.Segments.Add(new LineSegment { Point = PointOnArc(endAngle, InnerRadius) });

        figure.Segments.Add(new ArcSegment
        {
            Point = PointOnArc(startAngle, InnerRadius),
            Size = new(InnerRadius, InnerRadius),
            IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Counterclockwise,
        });

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static Point PointOnArc(double angle, double radius)
    {
        double radians = angle * Math.PI / 180;
        return new(Center + (radius * Math.Cos(radians)), Center + (radius * Math.Sin(radians)));
    }
}
