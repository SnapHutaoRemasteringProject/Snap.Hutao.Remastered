// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.Diagnostics;

internal static class ProcessExtension
{
    extension(IProcess process)
    {
        public bool IsRunning
        {
            get
            {
                try
                {
                    return !process.HasExited;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    return false;
                }
            }
        }

        public void SafeWaitForExit()
        {
            try
            {
                process.WaitForExit();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        }
    }
}