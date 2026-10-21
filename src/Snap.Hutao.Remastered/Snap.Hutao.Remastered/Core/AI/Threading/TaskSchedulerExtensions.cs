// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.Core.AI.Threading;

internal static class TaskSchedulerExtensions
{
    extension(TaskScheduler taskScheduler)
    {
        public Task Run(Action action, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.None, taskScheduler);
        }

        public Task Run(Action action, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(action, cancellationToken, taskCreationOptions, taskScheduler);
        }

        public Task Run<T>(Action<T> action, T t, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(static (stateObject) =>
            {
                (Action<T> action, T t) = ((Action<T>, T))stateObject!;
                action(t);
            }, (action, t), cancellationToken, TaskCreationOptions.None, taskScheduler);
        }

        public Task Run<T>(Action<T> action, T t, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(static (stateObject) =>
            {
                (Action<T> action, T t) = ((Action<T>, T))stateObject!;
                action(t);
            }, (action, t), cancellationToken, taskCreationOptions, taskScheduler);
        }

        public Task<TResult> Run<T, TResult>(Func<T, TResult> func, T t, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(static (stateObject) =>
            {
                (Func<T, TResult> func, T t) = ((Func<T, TResult>, T))stateObject!;
                return func(t);
            }, (func, t), cancellationToken, TaskCreationOptions.None, taskScheduler);
        }

        public Task<TResult> Run<T, TResult>(Func<T, TResult> func, T t, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(static (stateObject) =>
            {
                (Func<T, TResult> func, T t) = ((Func<T, TResult>, T))stateObject!;
                return func(t);
            }, (func, t), cancellationToken, taskCreationOptions, taskScheduler);
        }

        public Task Run<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(static (stateObject) =>
            {
                (Action<T1, T2> action, T1 t1, T2 t2) = ((Action<T1, T2>, T1, T2))stateObject!;
                action(t1, t2);
            }, (action, t1, t2), cancellationToken, TaskCreationOptions.None, taskScheduler);
        }

        public Task Run<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(static (stateObject) =>
            {
                (Action<T1, T2> action, T1 t1, T2 t2) = ((Action<T1, T2>, T1, T2))stateObject!;
                action(t1, t2);
            }, (action, t1, t2), cancellationToken, taskCreationOptions, taskScheduler);
        }
    }
}
