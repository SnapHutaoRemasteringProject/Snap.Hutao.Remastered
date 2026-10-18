// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public readonly record struct BackdropOverrideState<T>(bool IsActive, T Previous, T Current)
{
    public BackdropOverrideState<T> Select(bool useGlass, T none)
    {
        if (useGlass) return new(true, IsActive ? Previous : Current, none);
        return IsActive ? new(false, Previous, Previous) : this;
    }
}
