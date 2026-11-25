// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Service.DailyNote;
using System.Globalization;
using System.IO;

namespace Snap.Hutao.Remastered.Core.LifeCycle;

/// <summary>
/// 计划任务触发的无 UI 磁贴刷新入口。
/// 磁贴 API 要求进程有 MSIX 包身份，因此任务通过 AUMID 激活应用并传入 <see cref="Argument"/>；
/// 这条路径跳过 XAML，刷新完成后立即退出。
/// </summary>
internal static class TileRefreshActivation
{
    internal const string Argument = "--refresh-tile";

    /// <summary>
    /// 本次刷新的结果码，由等待中的脚本读取后转成计划任务的退出码。
    /// 不能依赖进程退出码：AUMID 激活的调用方拿不到被激活进程的退出码。
    /// 值必须与 <c>ActivateTileRefresh.ps1</c> 约定的区段一致（0 成功，非 0 失败），
    /// 且不能是 1——1 已被「脚本宿主/编译失败」占用。
    /// </summary>
    private const string ResultFileName = "TileRefresh.last";

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    public static bool IsRequested(string[] args)
    {
        return args.Contains(Argument, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行一次无 UI 刷新，调用方随后应立即退出进程。
    /// </summary>
    public static void RunHeadless()
    {
        int exitCode = 1;
        string? detail = null;

        try
        {
            if (!ShouldRunHeadless())
            {
                // 主实例在运行：它自己的定时任务已负责刷新，这里再跑只会争抢 SQLite 写锁。
                // 对计划任务而言这是成功（无事可做不是错误）。
                WriteResult(0, "skipped: main instance running");
                return;
            }

            // 刻意不用 using 释放容器：AppNotificationLifeTime.Dispose 会清空用户的全部系统通知。
            // 进程随即退出，资源交给操作系统回收。
            // 类型全限定：命名空间与静态类同名会产生二义。
            ServiceProvider serviceProvider = Core.DependencyInjection.DependencyInjection.Initialize();
            ITileRefreshRunner runner = serviceProvider.GetRequiredService<ITileRefreshRunner>();

            using CancellationTokenSource timeout = new(Timeout);
            try
            {
                TileRefreshResult result = runner.RunAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
                exitCode = result.Outcome switch
                {
                    TileRefreshOutcome.Succeeded => 0,
                    TileRefreshOutcome.VerificationRequired => 4,
                    _ => 1,
                };

                detail = result.Failure is null
                    ? $"outcome={result.Outcome}"
                    : $"outcome={result.Outcome}: {result.Failure}";
            }
            catch (OperationCanceledException)
            {
                exitCode = 2;
                detail = "timeout";
            }
        }
        catch (Exception ex)
        {
            // 容器初始化或刷新未预期地抛出：记录下来，让计划的退出码反映失败。
            exitCode = 3;
            WriteResult(exitCode, ex.ToString());
            Environment.ExitCode = exitCode;
            return;
        }

        WriteResult(exitCode, detail);
        Environment.ExitCode = exitCode;
    }

    /// <summary>
    /// 判断「本用户的实例」是否已在运行。
    /// 不能按进程名判断：那会把其他 Windows 用户打开的副本也当成主实例，
    /// 导致本用户的计划任务刷新被无谓跳过。
    /// 只能用 <see cref="AppInstance.GetInstances"/> 查询，绝不能用
    /// <c>FindOrRegisterForKey</c>：后者会把主实例键短暂注册到 headless 进程上，
    /// 期间用户正常启动会被重定向到本进程——而 headless 不装 Activated 处理器，
    /// 那次激活会被吞掉，窗口不开。
    /// </summary>
    private static bool ShouldRunHeadless()
    {
        string instanceKey = "Snap.Hutao.Remastered" + AppDataPaths.InstanceSuffix;

        // 枚举命名实例，只读不注册。
        foreach (AppInstance instance in AppInstance.GetInstances())
        {
            if (instance.Key == instanceKey && !instance.IsCurrent)
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteResult(int exitCode, string? detail = null)
    {
        try
        {
            string path = Path.Combine(HutaoRuntime.LocalFolderDirectory, ResultFileName);
            string content = exitCode.ToString(CultureInfo.InvariantCulture);
            if (detail is not null)
            {
                content += Environment.NewLine + detail;
            }

            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            // 结果文件只影响计划任务的退出码上报，写不进去不该让刷新本身失败；
            // 但要留下记录，否则「任务显示成功/失败原因未知」无法排查。
            SentrySdk.CaptureException(ex);
        }
    }
}