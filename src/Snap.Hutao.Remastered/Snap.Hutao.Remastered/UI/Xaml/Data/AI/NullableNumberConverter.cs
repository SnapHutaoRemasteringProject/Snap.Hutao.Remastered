// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.AI;

internal sealed class NullableNumberConverter : IValueConverter
{
    public NullableNumberType ValueType { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
        {
            return double.NaN;
        }

        return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is not double number || double.IsNaN(number))
        {
            return null;
        }

        if (ValueType == NullableNumberType.Int32)
        {
            return ConvertToNullableInt32(number);
        }

        if (ValueType == NullableNumberType.Single)
        {
            return (float?)number;
        }

        if (ValueType == NullableNumberType.Double)
        {
            return (double?)number;
        }

        return number;
    }

    private static int? ConvertToNullableInt32(double value)
    {
        if (value <= 0)
        {
            return null;
        }

        if (value >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
