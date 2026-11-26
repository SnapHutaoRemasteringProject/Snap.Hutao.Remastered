// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.Island;

internal static class IslandFrameRate
{
    internal static int SelectTarget(
        bool frameRateEnabled,
        int foregroundTargetFps,
        bool backgroundEnabled,
        int backgroundTargetFps,
        int gameProcessId,
        uint foregroundProcessId)
    {
        // A missing foreground window can occur during activation or on another desktop.
        if (frameRateEnabled && backgroundEnabled && backgroundTargetFps is >= 1 and <= 2000
            && gameProcessId > 0 && foregroundProcessId != 0 && foregroundProcessId != (uint)gameProcessId)
        {
            return backgroundTargetFps;
        }

        return foregroundTargetFps;
    }
}
