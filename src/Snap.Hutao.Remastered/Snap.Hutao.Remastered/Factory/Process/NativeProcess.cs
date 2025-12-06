// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;

namespace Snap.Hutao.Remastered.Factory.Process;

internal sealed partial class NativeProcess : IProcess
{
    private readonly HutaoNativeProcess process;

    public NativeProcess(HutaoNativeProcess process)
    {
        this.process = process;
    }

    public int Id { get => (int)process.Id; }

    public nint Handle { get => process.ProcessHandle; }

    public HWND MainWindowHandle { get => process.MainWindowHandle; }

    public bool HasExited { get => process.GetExitCodeProcess(out _); }

    public int ExitCode
    {
        get
        {
            process.GetExitCodeProcess(out uint code);
            return (int)code;
        }
    }

    public void Start()
    {
        process.Start();
    }

    public void ResumeMainThread()
    {
        process.ResumeMainThread();
    }

    public void WaitForExit()
    {
        process.WaitForExit();
    }

    public void Kill()
    {
        process.Kill();
    }

    public void Dispose()
    {
    }
}