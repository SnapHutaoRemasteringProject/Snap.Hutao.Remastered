// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;

namespace Snap.Hutao.Remastered.Core.Threading;

public static class TaskExtension
{
    extension(Task task)
    {
        public async void SafeForget()
        {
            try
            {
                // This sink only observes/logs failures. Do not post its
                // continuation to a XAML dispatcher that may be shutting down.
                await task.ConfigureAwait(false);
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
                await task.ConfigureAwait(false);
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
