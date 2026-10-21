// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.ComponentModel;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;

namespace Snap.Hutao.Remastered.Service.AI;

internal static class BuiltInFunctions
{
    [Description("Get the current local time.")]
    public static string GetCurrentTime()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.AIToolCurrentTime, "Get current local time");

        try
        {
            return DateTimeOffset.Now.ToString("O");
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.AIToolCurrentTime);
            throw;
        }
    }

    [Description("Show a summary of the conversation or context.")]
    public static string ShowSummary(string summary)
    {
        return summary;
    }
}
