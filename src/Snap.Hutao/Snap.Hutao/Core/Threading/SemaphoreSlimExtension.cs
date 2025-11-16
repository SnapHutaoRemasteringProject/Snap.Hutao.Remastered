// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;

namespace Snap.Hutao.Core.Threading;

internal static class SemaphoreSlimExtension
{
    extension(SemaphoreSlim semaphoreSlim)
    {
        public async ValueTask<SemaphoreSlimToken> EnterAsync(CancellationToken token = default)
        {
            try
            {
                await semaphoreSlim.WaitAsync(token).ConfigureAwait(false);
            }
            catch (ObjectDisposedException ex)
            {
                HutaoException.OperationCanceled(SH.CoreThreadingSemaphoreSlimDisposed, ex);
            }

            return new(semaphoreSlim);
        }

        public SemaphoreSlimToken Enter()
        {
            try
            {
                semaphoreSlim.Wait();
            }
            catch (ObjectDisposedException ex)
            {
                HutaoException.OperationCanceled(SH.CoreThreadingSemaphoreSlimDisposed, ex);
            }

            return new(semaphoreSlim);
        }
    }
}