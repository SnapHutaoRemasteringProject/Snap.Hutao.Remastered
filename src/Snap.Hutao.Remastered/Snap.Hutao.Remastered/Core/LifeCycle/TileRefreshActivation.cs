// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Service.DailyNote;

namespace Snap.Hutao.Remastered.Core.LifeCycle;

/// <summary>
/// 计划任务触发的无 UI 磁贴刷新入口。
/// 磁贴 API 要求进程有 MSIX 包身份，因此任务通过 AUMID 激活应用并传入 <see cref="Argument"/>；
/// 这条路径跳过 XAML，刷新完成后立即退出。
/// </summary>
internal static class TileRefreshActivation
{
    internal const string Argument = "--refresh-tile";

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
        if (!ShouldRunHeadless())
        {
            // 主实例在运行：它自己的定时任务已负责刷新，这里再跑只会争抢 SQLite 写锁。
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
            Environment.ExitCode = runner.RunAsync(timeout.Token).AsTask().GetAwaiter().GetResult() ? 0 : 1;
        }
        catch (OperationCanceledException)
        {
            Environment.ExitCode = 2;
        }
    }

    /// <summary>
    /// 用与应用主入口相同的单实例键判断「本用户的实例」是否已在运行。
    /// 不能按进程名判断：那会把其他 Windows 用户打开的副本也当成主实例，
    /// 导致本用户的计划任务刷新被无谓跳过。AppInstance 键是每用户独立的，
    /// 与用户名一起派生，恰好表达「本用户的主实例」。
    /// </summary>
    private static bool ShouldRunHeadless()
    {
        string instanceKey = "Snap.Hutao.Remastered" + AppDataPaths.InstanceSuffix;
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey(instanceKey);
        try
        {
            // keyInstance 即当前进程（headless 启动时无人持有该键）；
            // 若已被主实例注册，IsCurrent 为 false。
            return keyInstance.IsCurrent;
        }
        finally
        {
            if (keyInstance.IsCurrent)
            {
                keyInstance.UnregisterKey();
            }
        }
    }
}