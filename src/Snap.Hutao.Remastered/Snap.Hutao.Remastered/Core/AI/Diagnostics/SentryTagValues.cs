// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.Core.AI.Diagnostics;

internal static class SentryTagValues
{
    public const string False = "false";
    public const string True = "true";

    public static string FromBoolean(bool value)
    {
        return value ? True : False;
    }
}
