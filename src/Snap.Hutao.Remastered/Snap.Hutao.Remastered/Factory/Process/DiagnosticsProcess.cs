// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32.SafeHandles;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.Runtime.InteropServices;
using Win32Handle = global::Windows.Win32.Foundation.HANDLE;
using Win32PInvoke = global::Windows.Win32.PInvoke;
using Win32ProcessAccess = global::Windows.Win32.System.Threading.PROCESS_ACCESS_RIGHTS;

namespace Snap.Hutao.Remastered.Factory.Process;

public sealed partial class DiagnosticsProcess : IProcess
{
    private readonly System.Diagnostics.Process process;
    private ProcessWaitHandle? exitHandle;

    public DiagnosticsProcess(System.Diagnostics.Process process)
    {
        this.process = process;
    }

    public int Id { get => process.Id; }

    public nint Handle { get => process.Handle; }

    public HWND MainWindowHandle { get => process.MainWindowHandle; }

    public bool HasExited
    {
        get
        {
            try
            {
                // On Windows, Process.HasExited may treat an inaccessible handle as an exit.
                return GetExitHandle()?.WaitOne(0) ?? true;
            }
            catch (InvalidOperationException)
            {
                // No process is associated with this object.
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_ACCESS_DENIED)
            {
                return false;
            }
        }
    }

    public int ExitCode { get => process.ExitCode; }

    public void Start()
    {
        process.Start();
    }

    public void ResumeMainThread()
    {
        HutaoException.NotSupported("ResumeMainThread is not supported for System.Diagnostics.Process.");
    }

    public void WaitForExit()
    {
        process.WaitForExit();
    }

    public async Task WaitForExitAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await WaitForExitAsync(GetExitHandle(), token).ConfigureAwait(false);
    }

    internal async Task KillAndWaitForExitAsync(CancellationToken token)
    {
        if (HasExited)
        {
            return;
        }

        using ProcessWaitHandle? handle = OpenWaitHandle(Win32ProcessAccess.PROCESS_SYNCHRONIZE | Win32ProcessAccess.PROCESS_TERMINATE);
        if (handle is null || handle.WaitOne(0))
        {
            return;
        }

        if (!Win32PInvoke.TerminateProcess(new Win32Handle(handle.SafeWaitHandle.DangerousGetHandle()), uint.MaxValue))
        {
            int error = Marshal.GetLastPInvokeError();
            if (handle.WaitOne(0))
            {
                return;
            }

            throw new Win32Exception(error);
        }

        await WaitForExitAsync(handle, token).ConfigureAwait(false);
    }

    public void Kill()
    {
        process.Kill();
    }

    public void Dispose()
    {
        exitHandle?.Dispose();
        process.Dispose();
    }

    private ProcessWaitHandle? GetExitHandle()
    {
        return exitHandle ??= OpenWaitHandle(Win32ProcessAccess.PROCESS_SYNCHRONIZE);
    }

    private unsafe ProcessWaitHandle? OpenWaitHandle(Win32ProcessAccess access)
    {
        Win32Handle handle = Win32PInvoke.OpenProcess(access, false, (uint)Id);
        if (!handle.IsNull)
        {
            return new(new SafeWaitHandle((nint)handle.Value, ownsHandle: true));
        }

        int error = Marshal.GetLastPInvokeError();
        if (error == (int)WIN32_ERROR.ERROR_INVALID_PARAMETER)
        {
            // The process disappeared between the snapshot and OpenProcess.
            return null;
        }

        throw new Win32Exception(error);
    }

    private static async Task WaitForExitAsync(ProcessWaitHandle? handle, CancellationToken token)
    {
        if (handle is null || handle.WaitOne(0))
        {
            return;
        }

        TaskCompletionSource exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisteredWaitHandle registration = ThreadPool.RegisterWaitForSingleObject(handle,
            static (state, _) => ((TaskCompletionSource)state!).TrySetResult(), exited, Timeout.Infinite, executeOnlyOnce: true);
        try
        {
            await exited.Task.WaitAsync(token).ConfigureAwait(false);
        }
        finally
        {
            registration.Unregister(null);
        }
    }

    private sealed class ProcessWaitHandle : WaitHandle
    {
        public ProcessWaitHandle(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }
    }
}
