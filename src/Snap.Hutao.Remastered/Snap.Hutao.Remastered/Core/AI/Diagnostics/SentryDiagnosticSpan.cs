// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Sentry;
using System.Collections.Generic;
using System.Threading;

namespace Snap.Hutao.Remastered.Core.AI.Diagnostics;

internal sealed class SentryDiagnosticSpan(ISpan span) : IDisposable
{
    private readonly ISpan span = span;
    private bool finished;

    public IReadOnlyDictionary<string, string> Tags { get => span.Tags; }

    public void SetTag(string key, string value)
    {
        span.SetTag(key, value);
    }

    public void SetTag(string key, bool value)
    {
        SetTag(key, SentryTagValues.FromBoolean(value));
    }

    public void SetData(string key, object? value)
    {
        span.SetData(key, value);
    }

    public void Finish()
    {
        if (Interlocked.Exchange(ref finished, true))
        {
            return;
        }

        span.Finish();
    }

    public void Finish(SpanStatus status)
    {
        if (Interlocked.Exchange(ref finished, true))
        {
            return;
        }

        span.Finish(status);
    }

    public void Finish(Exception exception)
    {
        if (Interlocked.Exchange(ref finished, true))
        {
            return;
        }

        span.Finish(exception);
    }

    public void Dispose()
    {
        Finish();
    }
}
