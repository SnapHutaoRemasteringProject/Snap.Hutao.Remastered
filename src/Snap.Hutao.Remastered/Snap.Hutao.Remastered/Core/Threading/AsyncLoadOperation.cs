// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.Threading;

/// <summary>Shares an in-flight load and retains success; a failure can be retried immediately.</summary>
public sealed class AsyncLoadOperation
{
    private readonly object gate = new();
    private Task<bool>? pending;

    public Task<bool> RunAsync(Func<CancellationToken, Task<bool>> load, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        TaskCompletionSource<bool> completion;
        lock (gate)
        {
            if (pending is { } current && (!current.IsCompleted
                || (current.IsCompletedSuccessfully && current.Result)))
            {
                return current.WaitAsync(token);
            }

            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            pending = completion.Task;
        }

        // Publish the task before invoking the loader: it may synchronously re-enter.
        // CompleteAsync transfers every outcome to completion; no async-void sink
        // (and therefore no captured XAML synchronization context) is needed here.
        _ = CompleteAsync(load, token, completion);
        return completion.Task;
    }

    private async Task CompleteAsync(Func<CancellationToken, Task<bool>> load, CancellationToken token, TaskCompletionSource<bool> completion)
    {
        try
        {
            bool success = await load(token).ConfigureAwait(false);
            lock (gate)
            {
                completion.TrySetResult(success);
            }
        }
        catch (OperationCanceledException)
        {
            lock (gate)
            {
                completion.TrySetCanceled(token);
            }
        }
        catch (Exception exception)
        {
            lock (gate)
            {
                completion.TrySetException(exception);
            }
        }
    }
}
