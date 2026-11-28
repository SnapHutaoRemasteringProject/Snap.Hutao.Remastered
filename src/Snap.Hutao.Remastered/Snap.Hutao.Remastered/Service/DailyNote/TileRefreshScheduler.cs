// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Windows.ApplicationModel;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 用 Windows 计划任务定时激活应用来刷新磁贴。
/// 磁贴 API 要求 MSIX 包身份，因此任务不能直接执行 exe，
/// 而是执行随包分发的脚本，由它通过 AUMID 激活应用并传入 <c>--refresh-tile</c>。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(ITileRefreshScheduler))]
internal sealed partial class TileRefreshScheduler : ITileRefreshScheduler
{
    internal const string TaskNamePrefix = "Snap Hutao Tile Refresh";

    /// <summary>
    /// 任务名带当前用户标识。计划任务在同一台机器上是共享的：多个 Windows 用户
    /// 各自开启后台刷新时，固定名字会让后开启者 <c>/F</c> 覆盖前者的任务，
    /// 也会在任一方关闭开关时删掉对方的任务。SID 哈希只用于区分用户，
    /// 不包含可识别的账户信息。
    /// </summary>
    internal static readonly string TaskName = BuildTaskName();

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

    /// <summary>
    /// 包装脚本的「刷新未成功」码：<c>5</c> 表示被激活的进程报告刷新失败，
    /// <c>6</c> 表示进程没有写出结果（异常退出），<c>7</c> 表示服务器要求人机验证。
    /// 三者都说明脚本与任务正常，失败发生在刷新过程中，因此与脚本不可用区分开。
    /// </summary>
    private const uint RefreshFailed = 5;
    private const uint RefreshResultMissing = 6;
    private const uint VerificationRequired = 7;

    private readonly ILogger<TileRefreshScheduler> logger;

    /// <summary>
    /// 串行化所有会改任务/暂存文件的操作（注册、删除、间隔同步）：
    /// 「改间隔时 Sync 先查询到任务存在，随后用户关闭开关删除了任务，
    /// Sync 的重建又跑在删除之后」这类交错会让开关显示关闭而任务仍在跑。
    /// </summary>
    private readonly AsyncLock taskGate = new();

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
            VerificationRequired => TileRefreshScheduleStatus.VerificationRequired,
            RefreshFailed or RefreshResultMissing => TileRefreshScheduleStatus.RefreshFailed,
            _ => TileRefreshScheduleStatus.Failed,
        };
    }

    public async ValueTask<bool> RegisterAsync(int intervalMinutes, CancellationToken token = default)
    {
        using (await taskGate.LockAsync().ConfigureAwait(false))
        {
            return await RegisterCoreAsync(intervalMinutes, token).ConfigureAwait(false);
        }
    }

    private async ValueTask<bool> RegisterCoreAsync(int intervalMinutes, CancellationToken token)
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
        // innerCommand 本身就是完整的 VBS 字符串字面量，因此必须自带开头和结尾各
        // 三个引号（"""path"""），少一个就会把字符串留成未闭合、编译失败。
        string innerCommand = $"\"\"\"{ResolvePowerShellPath()}\"\" -NoProfile -NonInteractive -ExecutionPolicy Bypass" +
            $" -File \"\"{scriptPath}\"\" -Aumid \"\"{Package.Current.Id.FamilyName}!App\"\" -TaskName \"\"{TaskName}\"\"\"";
        string wrapperContent = $"WScript.Quit CreateObject(\"WScript.Shell\").Run({innerCommand}, 0, True)\r\n";

        if (!TryWriteTextFile(wrapperPath, wrapperContent))
        {
            return false;
        }

        // wscript.exe 在 System32，路径固定且短。
        // //B（批处理模式）抑制脚本宿主的一切界面：脚本被删除或语法出错时不会弹
        // 「Windows Script Host」错误框阻塞任务，任务只是静默返回非零码。
        string taskCommand = $"\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wscript.exe")}\" //B \"{wrapperPath}\"";
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

        // 不再额外调 /Change /IT /RL LIMITED：/Create 建出来的任务本来就是
        // 「当前用户 + Interactive only」，实测直接可用。
        // 而 /Change 在自动化环境里会提示输入账户密码，标准输入被重定向时它直接挂住，
        // 等到超时被取消——任务其实已经建好了，界面却报注册失败。
        return true;
    }

    public async ValueTask<bool> UnregisterAsync(CancellationToken token = default)
    {
        using (await taskGate.LockAsync().ConfigureAwait(false))
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
    }

    public async ValueTask SyncIntervalAsync(int intervalMinutes, CancellationToken token = default)
    {
        using (await taskGate.LockAsync().ConfigureAwait(false))
        {
            // 改间隔不应悄悄把开关打开：任务未注册时直接跳过。
            // 查询与重建同持一把锁，不会被并发的 UnregisterAsync 插队。
            if (RuntimeEnvironment.IsUnpackaged || !await IsRegisteredAsync(token).ConfigureAwait(false))
            {
                return;
            }

            // 已注册则按新间隔重建（RegisterCoreAsync 内部 /F 覆盖同名任务）。
            bool synced = await RegisterCoreAsync(intervalMinutes, token).ConfigureAwait(false);
            if (!synced)
            {
                logger.LogWarning("Syncing tile refresh interval to {Minutes} minutes failed", intervalMinutes);
            }
        }
    }

    private static string BuildTaskName()
    {
        string? sid = WindowsIdentity.GetCurrent().User?.Value;
        if (string.IsNullOrEmpty(sid))
        {
            return TaskNamePrefix;
        }

        string suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sid)))[..8];
        return $"{TaskNamePrefix} {suffix}";
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
            // 必须写成 UTF-16 LE + BOM：wscript 会把无 BOM 的文件按 ANSI 解释，
            // 用户名含中文时（C:\Users\张三\...）包装脚本里的路径会乱码，
            // 后台刷新会静默失效。实测只有该编码能正确解析非 ASCII 路径。
            File.WriteAllText(path, content, Encoding.Unicode);
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

    private void TryDeleteStagedFiles()
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
        catch (Exception ex)
        {
            // 清理失败可诊断但不影响功能：下次注册会覆盖同一批文件。
            logger.LogWarning(ex, "Unable to delete staged tile refresh files under {Path}", HutaoRuntime.LocalFolderDirectory);
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

    private async ValueTask<(int ExitCode, string Output)> RunSchtasksAsync(string[] arguments, CancellationToken token)
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

    private void TryKill(Process process)
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
            // 进程已退出或无权结束，调用方继续抛原始异常；记录便于诊断。
            logger.LogDebug(ex, "Unable to kill schtasks process {Pid}", process.Id);
        }
    }
}