// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Diagnostics;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;
using Snap.Hutao.Win32.Foundation;

namespace Snap.Hutao.Factory.Process;

internal sealed partial class FullTrustProcess : IProcess
{
    private readonly FullTrustNamedPipeClient client = new();
    private global::System.Diagnostics.Process? process;

    public FullTrustProcess(FullTrustProcessStartInfoRequest startInfo)
    {
        client.Create(startInfo);
    }

    public int Id { get => process?.Id ?? throw HutaoException.InvalidOperation("Process not created"); }

    public nint Handle { get => process?.Handle ?? throw HutaoException.InvalidOperation("Process not created"); }

    public HWND MainWindowHandle { get => process?.MainWindowHandle ?? throw HutaoException.InvalidOperation("Process not created"); }

    public bool HasExited { get => process?.HasExited ?? throw HutaoException.InvalidOperation("Process not created"); }

    public int ExitCode { get => process?.ExitCode ?? throw HutaoException.InvalidOperation("Process not created"); }

    public void Dispose()
    {
        client.Dispose();
        process?.Dispose();
    }

    public void Kill()
    {
        if (process is null)
        {
            throw HutaoException.InvalidOperation("Process not created");
        }

        process.Kill();
    }

    public void ResumeMainThread()
    {
        client.ResumeMainThread();
    }

    public void Start()
    {
        uint processId = client.StartProcess();
        process = global::System.Diagnostics.Process.GetProcessById((int)processId);
    }

    public void WaitForExit()
    {
        if (process is null)
        {
            throw HutaoException.InvalidOperation("Process not created");
        }

        process.WaitForExit();
    }

    internal void LoadLibrary(FullTrustLoadLibraryRequest request)
    {
        client.LoadLibrary(request);
    }
}