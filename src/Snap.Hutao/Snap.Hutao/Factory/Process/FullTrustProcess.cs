// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Diagnostics;
using Snap.Hutao.Win32.Foundation;

namespace Snap.Hutao.Factory.Process;

internal sealed class FullTrustProcess : IProcess
{
    public int Id { get; }
    public nint Handle { get; }
    public HWND MainWindowHandle { get; }
    public bool HasExited { get; }
    public int ExitCode { get; }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Kill()
    {
        throw new NotImplementedException();
    }

    public void ResumeMainThread()
    {
        throw new NotImplementedException();
    }

    public void Start()
    {
        throw new NotImplementedException();
    }

    public void WaitForExit()
    {
        throw new NotImplementedException();
    }
}