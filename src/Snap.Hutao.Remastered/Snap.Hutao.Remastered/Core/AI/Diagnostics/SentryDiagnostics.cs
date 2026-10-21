// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Sentry;
using System.Collections.Generic;

namespace Snap.Hutao.Remastered.Core.AI.Diagnostics;

internal static class SentryDiagnostics
{
    public static void CaptureUnhandledException(Exception exception, bool isTerminating)
    {
        CaptureException(exception, SentryOperations.AppUnhandledException, scope =>
        {
            scope.SetTag(SentryTags.ExceptionIsTerminating, SentryTagValues.FromBoolean(isTerminating));
        });

        if (isTerminating)
        {
            SentrySdk.Flush(TimeSpan.FromSeconds(2));
        }
    }

    public static SentryDiagnosticSpan StartSpan(string operation, string description)
    {
        return new(SentrySdk.StartSpan(operation, description));
    }

    public static void AddBreadcrumb(string message, string category, string type, IDictionary<string, string>? data = null, BreadcrumbLevel level = BreadcrumbLevel.Info)
    {
        SentrySdk.AddBreadcrumb(message, category, type, data, level);
    }

    public static void CaptureException(Exception exception, string operation)
    {
        CaptureException(exception, operation, null);
    }

    public static void CaptureException(Exception exception, SentryDiagnosticSpan span, string operation)
    {
        CaptureException(exception, span, operation, null);
    }

    public static void CaptureException(Exception exception, SentryDiagnosticSpan span, string operation, Action<Scope>? configureScope)
    {
        Dictionary<string, string> tags = new(span.Tags);
        span.Finish(exception);
        CaptureException(exception, operation, scope =>
        {
            foreach (KeyValuePair<string, string> tag in tags)
            {
                scope.SetTag(tag.Key, tag.Value);
            }

            configureScope?.Invoke(scope);
        });
    }

    public static void CaptureException(Exception exception, string operation, Action<Scope>? configureScope)
    {
        SentrySdk.CaptureException(exception, scope =>
        {
            scope.SetTag(SentryTags.DiagnosticsOperation, operation);
            configureScope?.Invoke(scope);
        });
    }
}
