// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.IO;

namespace Snap.Hutao.Remastered.Core.AI.IO;

internal static class WellKnownLocations
{
    public static string Settings { get => Path.Combine(HutaoRuntime.DataDirectory, "AccountInsights", "Agent"); }
}
