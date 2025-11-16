// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;

namespace Snap.Hutao.Core.Threading;

[SuppressMessage("", "SH003")]
internal static class TaskContextExtension
{
    extension(ITaskContext taskContext)
    {
        public Task<T> InvokeOnMainThreadAsync<T>(Func<T> func)
        {
            return taskContext.DispatcherQueue.EnqueueAsync(func);
        }

        public Task InvokeOnMainThreadAsync(Action func)
        {
            return taskContext.DispatcherQueue.EnqueueAsync(func);
        }

#if DEBUG

        [Obsolete("Do not pass a function that returns a Task<T> to InvokeOnMainThreadAsync method", true)]
        public Task<Task<T>> InvokeOnMainThreadAsync<T>(Func<Task<T>> func)
        {
            return Task.FromException<Task<T>>(new NotSupportedException());
        }

        [Obsolete("Do not pass a function that returns a Task to InvokeOnMainThreadAsync method", true)]
        public Task<Task> InvokeOnMainThreadAsync(Func<Task> func)
        {
            return Task.FromException<Task>(new NotSupportedException());
        }

        [Obsolete("Do not pass a function that returns a ValueTask<T> to InvokeOnMainThreadAsync method", true)]
        public Task<ValueTask<T>> InvokeOnMainThreadAsync<T>(Func<ValueTask<T>> func)
        {
            return Task.FromException<ValueTask<T>>(new NotSupportedException());
        }

        [Obsolete("Do not pass a function that returns a ValueTask to InvokeOnMainThreadAsync method", true)]
        public Task<ValueTask> InvokeOnMainThreadAsync(Func<ValueTask> func)
        {
            return Task.FromException<ValueTask>(new NotSupportedException());
        }
#endif
    }
}