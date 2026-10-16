// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.ViewModel.Complex;

public partial class RateAndDelta : ObservableObject
{
    public RateAndDelta(double rate, double? lastRate)
    {
        Rate = $"{rate:P3}";
        RateDelta = lastRate.TryGetValue(out double lastRateValue) ? FormatDelta(rate - lastRateValue) : string.Empty;
    }

    public string Rate { get; private set => SetProperty(ref field, value); }

    public string RateDelta { get; private set => SetProperty(ref field, value); }

    public static RateAndDelta Create((double Rate, double? LastRate) tuple)
    {
        return new RateAndDelta(tuple.Rate, tuple.LastRate);
    }

    public static RateAndDelta Create((double Rate, double LastRate) tuple)
    {
        return new RateAndDelta(tuple.Rate, tuple.LastRate);
    }

    private static string FormatDelta(double value)
    {
        return value switch
        {
            >= 0 => $"+{value:P3}",
            < 0 => $"{value:P3}",
            _ => string.Empty,
        };
    }
}
