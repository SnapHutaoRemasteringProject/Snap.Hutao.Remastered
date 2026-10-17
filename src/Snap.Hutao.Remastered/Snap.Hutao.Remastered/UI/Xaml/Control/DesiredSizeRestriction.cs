// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

public static class DesiredSizeRestriction
{
    public static double Apply(double desired, double available, bool restricted, ref double retained)
    {
        if (!restricted || !double.IsFinite(desired))
        {
            retained = 0;
            return desired;
        }

        if (retained > available)
        {
            retained = 0;
        }

        retained = Math.Max(retained, desired);
        return double.IsFinite(available) ? Math.Min(retained, available) : retained;
    }
}
