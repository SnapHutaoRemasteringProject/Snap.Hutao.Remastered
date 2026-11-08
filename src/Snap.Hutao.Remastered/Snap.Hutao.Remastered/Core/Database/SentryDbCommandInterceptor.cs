// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore.Diagnostics;
using Snap.Hutao.Remastered.Core.Diagnostics;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Core.Database;

/// <summary>
/// Turns every SQL statement executed while a Sentry transaction is active into a child span.
/// </summary>
/// <remarks>
/// This covers reads and writes that do not go through
/// <see cref="DbSetExtension"/> (raw SQL, transactions, migrations) without changing any
/// repository code. Statements executed outside a transaction are ignored on purpose: the
/// interceptor must not create a transaction per statement.
/// </remarks>
public sealed class SentryDbCommandInterceptor : DbCommandInterceptor
{
    private const int MaxStatementLength = 1024;

    private readonly ConditionalWeakTable<DbCommand, StrongBox<ISpan>> spans = new();

    private SentryDbCommandInterceptor()
    {
    }

    public static SentryDbCommandInterceptor Instance { get; } = new();

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        StartSpan(command, eventData);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        StartSpan(command, eventData);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        TakeSpan(command)?.Finish();
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        TakeSpan(command)?.Finish();
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        StartSpan(command, eventData);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        StartSpan(command, eventData);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        TakeSpan(command)?.Finish();
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        TakeSpan(command)?.Finish();
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        StartSpan(command, eventData);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StartSpan(command, eventData);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        ISpan? span = TakeSpan(command);
        span?.SetData("db.rows_affected", result);
        span?.Finish();
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        ISpan? span = TakeSpan(command);
        span?.SetData("db.rows_affected", result);
        span?.Finish();
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        TakeSpan(command)?.Finish(eventData.Exception);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        TakeSpan(command)?.Finish(eventData.Exception);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private void StartSpan(DbCommand command, CommandEventData eventData)
    {
        ISpan? parent = SentrySpanScope.Current;
        if (parent is null)
        {
            return;
        }

        string statement = command.CommandText;
        ISpan span = parent.StartChild("db.sql", $"{eventData.ExecuteMethod} {GetStatementKind(statement)}");

        span.SetData("db.system", "sqlite");
        span.SetData("db.operation", eventData.ExecuteMethod.ToString());

        if (!string.IsNullOrWhiteSpace(statement))
        {
            span.SetData("db.statement", statement.Length > MaxStatementLength ? statement[..MaxStatementLength] : statement);
        }

        // A DbCommand instance is not reused concurrently; replacing a stale entry is still
        // cheaper than risking an Add on a key left behind by a canceled command.
        spans.Remove(command);
        spans.Add(command, new StrongBox<ISpan>(span));
    }

    private ISpan? TakeSpan(DbCommand command)
    {
        if (!spans.TryGetValue(command, out StrongBox<ISpan>? box))
        {
            return null;
        }

        spans.Remove(command);
        return box.Value;
    }

    private static string GetStatementKind(string statement)
    {
        ReadOnlySpan<char> trimmed = statement.AsSpan().TrimStart();
        int length = 0;
        while (length < trimmed.Length && char.IsLetter(trimmed[length]))
        {
            length++;
        }

        return length is 0 ? "SQL" : trimmed[..length].ToString().ToUpperInvariant();
    }
}
