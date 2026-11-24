// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Windows.ApplicationModel;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 用 Windows 计划任务定时激活应用来刷新磁贴。
/// 磁贴 API 要求 MSIX 包身份，因此任务不能直接执行 exe，
/// 而是执行随包分发的脚本，由它通过 AUMID 激活应用并传入 <c>--refresh-tile</c>。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(ITileRefreshScheduler))]
public sealed partial class TileRefreshScheduler : ITileRefreshScheduler
{
    internal const string TaskName = "Snap Hutao Tile Refresh";

    /// <summary>schtasks 对 <c>/TR</c> 的硬性长度上限，超限会直接拒绝创建任务。</summary>
    private const int MaxTaskCommandLength = 261;

    /// <summary>单次 schtasks 调用的上限，防止任务计划服务无响应时挂住调用方。</summary>
    private static readonly TimeSpan SchtasksTimeout = TimeSpan.FromSeconds(20);

    // schtasks 的 Last Result 是 Win32/HRESULT 结果码。按 uint 位模式比较，
    // 这样有符号与无符号输出（-2147024894 与 2147942402）都能命中同一个错误。
    private const uint SchtasksNotYetRun = 0x00041303;
    private const uint Win32FileNotFound = 0x80070002;
    private const uint Win32PathNotFound = 0x80070003;
    private const uint Win32SharingViolation = 0x80070020;
    private const uint WshDisabled = 0x80041305;

    /// <summary>
    /// wscript.exe 能启动但脚本缺失或 VBScript 执行失败时的退出码。
    /// 杀毒软件隔离脚本后落到这个值，同样属于「脚本不可用」。
    /// </summary>
    private const uint ScriptHostFailed = 1;

    private readonly ILogger<TileRefreshScheduler> logger;

    [GeneratedConstructor]
    public partial TileRefreshScheduler(IServiceProvider serviceProvider);

    public async ValueTask<bool> IsRegisteredAsync(CancellationToken token = default)
    {
        if (RuntimeEnvironment.IsUnpackaged)
        {
            return false;
        }

        (int exitCode, _) = await RunSchtasksAsync(["/Query", "/TN", TaskName], token).ConfigureAwait(false);
        return exitCode == 0;
    }

    public async ValueTask<TileRefreshScheduleStatus> GetStatusAsync(CancellationToken token = default)
    {
        if (RuntimeEnvironment.IsUnpackaged)
        {
            return TileRefreshScheduleStatus.NotRegistered;
        }

        (int exitCode, string output) = await RunSchtasksAsync(["/Query", "/TN", TaskName, "/FO", "LIST", "/V"], token).ConfigureAwait(false);
        if (exitCode != 0)
        {
            return TileRefreshScheduleStatus.NotRegistered;
        }

        // schtasks 的标签随系统语言变化，但结果码始终是纯数字，因此只认数字行。
        // 「Last Result」在输出中是最靠后的纯数字行之一，从末尾向前找可避开
        // 「Repeat: Every: 0 Hour(s)」这类含数字但非纯数字的行。
        long? result = null;
        string[] lines = output.Split('\n');
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i];
            int colon = line.IndexOf(':');
            if (colon < 0)
            {
                continue;
            }

            string value = line[(colon + 1)..].Trim();
            if (long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long parsed))
            {
                result = parsed;
                break;
            }
        }

        if (result is null)
        {
            return TileRefreshScheduleStatus.NotYetRun;
        }

        // 同一个错误可能以有符号（-2147942402）或无符号（2147942402）形式出现。
        // 先取 int 再转 uint，位模式与 Win32 结果码一致。
        uint code = unchecked((uint)(int)result.Value);
        return code switch
        {
            0 => TileRefreshScheduleStatus.Succeeded,
            SchtasksNotYetRun => TileRefreshScheduleStatus.NotYetRun,
            Win32FileNotFound or Win32PathNotFound or Win32SharingViolation or ScriptHostFailed
                => TileRefreshScheduleStatus.ScriptMissing,
            WshDisabled => TileRefreshScheduleStatus.ScriptHostDisabled,
            _ => TileRefreshScheduleStatus.Failed,
        };
    }

    public async ValueTask<bool> RegisterAsync(int intervalMinutes, CancellationToken token = default)
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("daily_note.tile_refresh_schedule.register", "daily_note_tile");

        if (RuntimeEnvironment.IsUnpackaged)
        {
            span.SetData("daily_note.tile_refresh_schedule.skipped_unpackaged", true);
            return false;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(intervalMinutes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(intervalMinutes, 1440);

        string bundledScript = Path.Combine(AppContext.BaseDirectory, "Resource", "Scripts", "ActivateTileRefresh.ps1");
        if (!File.Exists(bundledScript))
        {
            logger.LogWarning("Tile refresh script missing at {Path}", bundledScript);
            return false;
        }

        // 任务用 .vbs 包装器而不是直接引用完整命令：
        // /TR 上限 261 字符，而「wscript + 脚本路径 + AUMID」拼起来会超限。
        // 选 VBS 而不是 .bat：WScript.Shell.Run 的窗口模式参数为 0 时完全不创建控制台窗口，
        // 而 .bat 即使加了 @echo off 也会闪一下 cmd 窗口。
        // 文件落在 LocalFolder 而不是 %LOCALAPPDATA% 根：后者会被 MSIX 写入虚拟化重定向到
        // LocalCache，而不带包身份的计划任务读的是真实路径，会找不到脚本。
        string stagedDirectory = HutaoRuntime.LocalFolderDirectory;
        string scriptPath = Path.Combine(stagedDirectory, "TileRefresh.ps1");
        string wrapperPath = Path.Combine(stagedDirectory, "TileRefresh.vbs");

        // 早期版本用 .bat 包装器，会在任务运行时闪出控制台窗口。升级后一并清掉。
        string legacyBat = Path.Combine(stagedDirectory, "TileRefresh.bat");
        if (File.Exists(legacyBat))
        {
            TryDeleteFile(legacyBat);
        }

        if (!TryStageScript(bundledScript, stagedDirectory, scriptPath))
        {
            return false;
        }

        // VBS 字符串里的双引号要写成两个：外层一层转义引号，内层再包住路径。
        string innerCommand = $"\"\"\"{ResolvePowerShellPath()}\"\" -NoProfile -NonInteractive -ExecutionPolicy Bypass" +
            $" -File \"\"{scriptPath}\"\" -Aumid \"\"{Package.Current.Id.FamilyName}!App\"\"\"";
        string wrapperContent = $"CreateObject(\"WScript.Shell\").Run {innerCommand}, 0, False\r\n";

        if (!TryWriteTextFile(wrapperPath, wrapperContent))
        {
            return false;
        }

        // wscript.exe 在 System32，路径固定且短。
        string taskCommand = $"\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wscript.exe")}\" \"{wrapperPath}\"";
        span.SetData("daily_note.tile_refresh_schedule.command_length", taskCommand.Length);

        if (taskCommand.Length > MaxTaskCommandLength)
        {
            logger.LogWarning("Tile refresh command too long ({Length} > {Max})", taskCommand.Length, MaxTaskCommandLength);
            return false;
        }

        (int exitCode, string output) = await RunSchtasksAsync(
        [
            "/Create",
            "/TN", TaskName,
            "/TR", taskCommand,
            "/SC", "MINUTE",
            "/MO", intervalMinutes.ToString(),
            "/F",
        ], token).ConfigureAwait(false);

        if (exitCode != 0)
        {
            logger.LogWarning("Creating tile refresh task failed with {ExitCode}: {Output}", exitCode, output);
            span.SetData("daily_note.tile_refresh_schedule.created", false);
            return false;
        }

        span.SetData("daily_note.tile_refresh_schedule.created", true);

        // 磁贴是用户级 Shell 资源，任务必须登录后运行。
        (int settingsExit, string settingsOutput) = await RunSchtasksAsync(
        [
            "/Change",
            "/TN", TaskName,
            "/IT",
            "/RL", "LIMITED",
        ], token).ConfigureAwait(false);

        if (settingsExit != 0)
        {
            // 任务已创建，附加设置失败不影响刷新，仅记录。
            logger.LogWarning("Adjusting tile refresh task settings failed with {ExitCode}: {Output}", settingsExit, settingsOutput);
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return true;
    }

    public async ValueTask<bool> UnregisterAsync(CancellationToken token = default)
    {
        if (RuntimeEnvironment.IsUnpackaged)
        {
            return false;
        }

        (int exitCode, string output) = await RunSchtasksAsync(["/Delete", "/TN", TaskName, "/F"], token).ConfigureAwait(false);
        if (exitCode != 0 && !output.Contains("cannot find", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Deleting tile refresh task failed with {ExitCode}: {Output}", exitCode, output);
            return false;
        }

        TryDeleteStagedFiles();
        return true;
    }

    public async ValueTask SyncIntervalAsync(int intervalMinutes, CancellationToken token = default)
    {
        // 改间隔不应悄悄把开关打开：任务未注册时直接跳过。
        if (RuntimeEnvironment.IsUnpackaged || !await IsRegisteredAsync(token).ConfigureAwait(false))
        {
            return;
        }

        // 已注册则按新间隔重建（RegisterAsync 内部 /F 覆盖同名任务）。
        bool synced = await RegisterAsync(intervalMinutes, token).ConfigureAwait(false);
        if (!synced)
        {
            logger.LogWarning("Syncing tile refresh interval to {Minutes} minutes failed", intervalMinutes);
        }
    }

    private bool TryStageScript(string source, string directory, string destination)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.Copy(source, destination, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            // 必须捕获全部异常：漏掉任何一类都会留下「任务已建但脚本没落地」的静默失败。
            logger.LogWarning(ex, "Unable to stage tile refresh script at {Path}", destination);
            return false;
        }
    }

    private bool TryWriteTextFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to write tile refresh wrapper at {Path}", path);
            return false;
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to delete {Path}", path);
        }
    }

    private static void TryDeleteStagedFiles()
    {
        try
        {
            foreach (string name in (string[])["TileRefresh.ps1", "TileRefresh.vbs", "TileRefresh.bat"])
            {
                string staged = Path.Combine(HutaoRuntime.LocalFolderDirectory, name);
                if (File.Exists(staged))
                {
                    File.Delete(staged);
                }
            }
        }
        catch (Exception)
        {
            // 清理失败无需处理：下次注册会覆盖同一批文件。
        }
    }

    private static string ResolvePowerShellPath()
    {
        string pwsh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe");
        if (File.Exists(pwsh))
        {
            return pwsh;
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
    }

    private static async ValueTask<(int ExitCode, string Output)> RunSchtasksAsync(string[] arguments, CancellationToken token)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start schtasks.exe");

        // 两个流必须并发读：只读一个再等另一个会死锁。
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(token);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(token);

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(SchtasksTimeout);

        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return (process.ExitCode, await outputTask.ConfigureAwait(false) + await errorTask.ConfigureAwait(false));
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // 进程已退出或无权结束，调用方继续抛原始异常。
        }
    }
}