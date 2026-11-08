// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.Service.AI;

internal static class BuiltInFunctions
{
    [Description("Get the current local time.")]
    public static string GetCurrentTime()
    {
        try
        {
            return DateTimeOffset.Now.ToString("O");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            throw;
        }
    }

    [Description("Show a summary of the conversation or context.")]
    public static string ShowSummary(string summary)
    {
        return summary;
    }
}
