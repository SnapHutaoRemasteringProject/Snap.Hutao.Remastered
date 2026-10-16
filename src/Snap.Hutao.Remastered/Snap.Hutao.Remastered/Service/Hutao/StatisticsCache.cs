// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Snap.Hutao.Remastered.Service.Hutao;

public abstract class StatisticsCache
{
    private readonly ConcurrentDictionary<Type, TaskCompletionSource> typeToTcs = [];

    protected static IEnumerable<TResult> CurrentJoinLast<TElement, TKey, TResult>(IEnumerable<TElement> current, IEnumerable<TElement>? last, Func<TElement, TKey> keySelector, Func<TElement, TElement?, TResult> resultSelector)
        where TKey : notnull
    {
        if (last is null)
        {
            foreach (TElement element in current)
            {
                yield return resultSelector(element, default);
            }
        }
        else
        {
            Dictionary<TKey, TElement> lastMap = [];
            foreach (TElement element in last)
            {
                lastMap[keySelector(element)] = element;
            }

            foreach (TElement element in current)
            {
                yield return resultSelector(element, lastMap.GetValueOrDefault(keySelector(element)));
            }
        }
    }

    protected async ValueTask InitializeForTypeAsync<T, TContext>(TContext context, Func<TContext, Task> initialization)
    {
        TaskCompletionSource pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource current = typeToTcs.GetOrAdd(typeof(T), pending);
        if (!ReferenceEquals(current, pending))
        {
            await current.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            await initialization(context).ConfigureAwait(false);
            pending.TrySetResult();
        }
        catch (Exception exception)
        {
            // Complete every concurrent waiter, then allow a later navigation to retry.
            pending.TrySetException(exception);
            typeToTcs.TryRemove(new KeyValuePair<Type, TaskCompletionSource>(typeof(T), pending));
            _ = pending.Task.Exception;
            throw;
        }
    }
}
