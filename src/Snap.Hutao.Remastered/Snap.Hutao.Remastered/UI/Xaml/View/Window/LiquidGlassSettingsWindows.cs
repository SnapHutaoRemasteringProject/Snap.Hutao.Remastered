// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public static class LiquidGlassSettingsWindows
{
    private static LiquidGlassControlsWindow? controls;
    private static LiquidGlassEffectsWindow? effects;

    public static void OpenControls()
    {
        if (controls is null)
        {
            controls = new(Ioc.Default);
            controls.Closed += static (_, args) => { if (!args.Handled) controls = null; };
        }
        controls.Activate();
    }

    public static void OpenEffects()
    {
        if (effects is null)
        {
            effects = new(Ioc.Default);
            effects.Closed += static (_, args) => { if (!args.Handled) effects = null; };
        }
        effects.Activate();
    }
}
