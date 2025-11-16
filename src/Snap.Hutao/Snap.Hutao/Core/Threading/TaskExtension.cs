// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;

namespace Snap.Hutao.Core.Threading;

internal static class TaskExtension
{
    extension(Task task)
    {
        public async void SafeForget()
        {
            try
            {
                await task.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // Do nothing
            }
            catch (Exception ex)
            {
                ExceptionHandling.KillProcessOnDbExceptionNoThrow(ex);
                ex.SetSentryMechanism("TaskExtension.SafeForget", handled: true);
                SentrySdk.CaptureException(ex);
            }
        }
    }

    extension(ValueTask task)
    {
        public async void SafeForget()
        {
            try
            {
                await task.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // Do nothing
            }
            catch (Exception ex)
            {
                ExceptionHandling.KillProcessOnDbExceptionNoThrow(ex);
                ex.SetSentryMechanism("TaskExtension.SafeForget", handled: true);
                SentrySdk.CaptureException(ex);
            }
        }
    }
}