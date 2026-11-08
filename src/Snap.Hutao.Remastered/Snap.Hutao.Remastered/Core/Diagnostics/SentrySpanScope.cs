// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Core.Diagnostics;

/// <summary>
/// Owns a Sentry performance span for the lifetime of the <c>using</c> statement.
/// </summary>
/// <remarks>
/// <para>
/// Sentry's Business plan includes performance monitoring, but a desktop application has no
/// incoming requests, and <c>SentrySdk.StartTransaction</c> does not publish the new transaction
/// on the Sentry scope in the SDK version used here. The current span is therefore tracked in an
/// <see cref="AsyncLocal{T}"/> so child spans follow the caller's async flow, and concurrent
/// commands cannot attach their spans to each other's transaction.
/// </para>
/// <para>
/// <see cref="StartChild(string, string)"/> deliberately does nothing when there is no active
/// transaction, so an HTTP request or a SQL command executed by a background service cannot
/// silently become a transaction of its own.
/// </para>
/// </remarks>
public sealed class SentrySpanScope : IDisposable
{
    private static readonly AsyncLocal<ISpan?> CurrentSpan = new();
    private static readonly SentrySpanScope NoOp = new(null, null);

    private readonly ISpan? span;
    private readonly ISpan? parent;
    private bool isFinished;

    private SentrySpanScope(ISpan? span, ISpan? parent)
    {
        this.span = span;
        this.parent = parent;
    }

    public static ISpan? Current
    {
        get => CurrentSpan.Value is { IsFinished: false } span ? span : null;
    }

    public bool IsActive
    {
        get => span is not null && !isFinished;
    }

    public static SentrySpanScope StartTransaction(string name, string operation)
    {
        if (!SentrySdk.IsEnabled)
        {
            return NoOp;
        }

        ISpan? previous = CurrentSpan.Value;
        ITransactionTracer transaction = SentrySdk.StartTransaction(name, operation);
        CurrentSpan.Value = transaction;
        return new(transaction, previous);
    }

    public static SentrySpanScope StartCommand(string typeName, [CallerMemberName] string methodName = "")
    {
        return StartTransaction($"{typeName}.{methodName}", "ui.command");
    }

    /// <summary>
    /// Starts a transaction when no operation is being measured yet, otherwise a child span.
    /// Use this for service entry points that can be reached both from a UI command (where the
    /// span must be nested) and from a background job (where it is the top-level operation).
    /// </summary>
    public static SentrySpanScope StartOperation(string name, string operation)
    {
        return Current is null ? StartTransaction(name, operation) : StartChild(operation, name);
    }

    public static SentrySpanScope StartChild(string operation, string description)
    {
        if (!SentrySdk.IsEnabled)
        {
            return NoOp;
        }

        ISpan? parent = Current;
        if (parent is null)
        {
            return NoOp;
        }

        ISpan child = parent.StartChild(operation, description);
        CurrentSpan.Value = child;
        return new(child, parent);
    }

    public SentrySpanScope SetData(string key, object value)
    {
        if (span is null || isFinished)
        {
            return this;
        }

        span.SetData(key, value);
        return this;
    }

    public SentrySpanScope SetTag(string key, string value)
    {
        if (span is null || isFinished)
        {
            return this;
        }

        span.SetTag(key, value);
        return this;
    }

    /// <summary>
    /// Marks the span as failed. The exception is attached to the span; it is not captured as a
    /// separate Sentry event, the caller owns the error reporting policy.
    /// </summary>
    public SentrySpanScope SetException(Exception exception)
    {
        if (span is null || isFinished)
        {
            return this;
        }

        isFinished = true;
        span.Finish(exception);
        RestoreCurrent();
        return this;
    }

    public void Dispose()
    {
        if (span is null || isFinished)
        {
            return;
        }

        isFinished = true;
        span.Finish();
        RestoreCurrent();
    }

    private void RestoreCurrent()
    {
        if (ReferenceEquals(CurrentSpan.Value, span))
        {
            CurrentSpan.Value = parent;
        }
    }
}
