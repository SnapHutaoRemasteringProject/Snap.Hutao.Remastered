// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.FullTrust;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.IO;

namespace Snap.Hutao.Remastered.Factory.Process;

public sealed class ProcessFactory
{
    public static void KillCurrent()
    {
        global::System.Diagnostics.Process.GetCurrentProcess().Kill();
    }

    public static bool TryGetById(int processId, [NotNullWhen(true)] out IProcess? process)
    {
        try
        {
            process = new DiagnosticsProcess(global::System.Diagnostics.Process.GetProcessById(processId));
            return true;
        }
        catch (Exception ex)
        {
            // Process with an Id of $id$ is not running.
            if (ex is not ArgumentException)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        process = null;
        return false;
    }

    public static bool IsRunning(ReadOnlySpan<string> processNames, [NotNullWhen(true)] out IProcess? runningProcess)
    {
        List<DiagnosticsProcess> processes = GetRunningProcesses(processNames);
        runningProcess = processes.FirstOrDefault();
        for (int index = 1; index < processes.Count; index++)
        {
            processes[index].Dispose();
        }

        return runningProcess is not null;
    }

    internal static List<DiagnosticsProcess> GetRunningProcesses(ReadOnlySpan<string> processNames)
    {
        using global::System.Diagnostics.Process currentProcess = global::System.Diagnostics.Process.GetCurrentProcess();
        int currentSessionId = currentProcess.SessionId;
        List<DiagnosticsProcess> matches = [];
        foreach (string name in processNames)
        {
            foreach (global::System.Diagnostics.Process process in global::System.Diagnostics.Process.GetProcessesByName(name))
            {
                DiagnosticsProcess candidate = new(process);
                bool retained = false;
                try
                {
                    if (process.SessionId != currentSessionId)
                    {
                        continue;
                    }

                    if (candidate.HasExited)
                    {
                        continue;
                    }

                    matches.Add(candidate);
                    retained = true;
                }
                catch (InvalidOperationException)
                {
                    // A process can exit between enumeration and inspection; keep checking the others.
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
                finally
                {
                    if (!retained)
                    {
                        candidate.Dispose();
                    }
                }
            }
        }

        return matches;
    }

    public static IProcess CreateUsingShellExecuteRunAs(string arguments, string fileName, string workingDirectory)
    {
        global::System.Diagnostics.Process process = new()
        {
            StartInfo = new()
            {
                Arguments = arguments,
                FileName = fileName,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = workingDirectory,
            },
        };

        return new DiagnosticsProcess(process);
    }

    public static unsafe IProcess CreateSuspended(string arguments, string fileName, string workingDirectory)
    {
        fixed (char* pArguments = arguments)
        {
            fixed (char* pFileName = fileName)
            {
                fixed (char* pWorkingDirectory = workingDirectory)
                {
                    HutaoNativeProcessStartInfo startInfo = new()
                    {
                        ApplicationName = pFileName,
                        CommandLine = pArguments,
                        InheritHandles = BOOL.FALSE,
                        CreationFlags = Win32.System.Threading.PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
                        CurrentDirectory = pWorkingDirectory,
                    };

                    return new NativeProcess(HutaoNative.Instance.MakeProcess(startInfo));
                }
            }
        }
    }

    public static IProcess CreateUsingFullTrustSuspended(string arguments, string fileName, string workingDirectory)
    {
        string repoDirectory = HutaoRuntime.GetDataRepositoryDirectory();
        string fullTrustFilePath = Path.Combine(repoDirectory, "Snap.ContentDelivery", "Snap.Hutao.Remastered.FullTrust.exe");
        StartUsingShellExecuteRunAs(fullTrustFilePath);

        FullTrustProcessStartInfoRequest request = new()
        {
            ApplicationName = fileName,
            CommandLine = arguments,
            CreationFlags = Win32.System.Threading.PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
            CurrentDirectory = workingDirectory,
        };

        return new FullTrustProcess(request);
    }

    public static void StartUsingShellExecute(string arguments, string fileName)
    {
        global::System.Diagnostics.Process.Start(new global::System.Diagnostics.ProcessStartInfo
        {
            Arguments = arguments,
            FileName = fileName,
            UseShellExecute = true,
        });
    }

    public static void StartUsingShellExecuteRunAs(string fileName)
    {
        StartUsingShellExecuteRunAs(fileName, string.Empty);
    }

    public static void StartUsingShellExecuteRunAs(string fileName, string arguments)
    {
        global::System.Diagnostics.Process.Start(new global::System.Diagnostics.ProcessStartInfo
        {
            Arguments = arguments,
            FileName = fileName,
            UseShellExecute = true,
            Verb = "runas",
        });
    }
}
