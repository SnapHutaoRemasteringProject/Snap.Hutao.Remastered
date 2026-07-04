// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

internal static class GamePackageOperationSpeedGraphPlayback
{
    internal static async Task RunAsync(ISpeedGraph speedGraph, Action<string> log, Func<TimeSpan, Task>? delayProvider = null, CancellationToken cancellationToken = default)
    {
        delayProvider ??= static _ => Task.CompletedTask;

        ulong maxSpeed = 42;
        long lastUpdateTimestamp = 12345;

        log("步骤 1：重置图表");
        GamePackageOperationSpeedGraphHelper.ResetSpeedGraph(speedGraph, ref maxSpeed, ref lastUpdateTimestamp);
        log($"图表已重置：最大速度 = {maxSpeed}，最后更新时间戳 = {lastUpdateTimestamp}");
        await delayProvider(TimeSpan.FromMilliseconds(700));
        cancellationToken.ThrowIfCancellationRequested();

        log("步骤 2：总字节数为 0，触发重置");
        GamePackageOperationSpeedGraphHelper.UpdateSpeedGraph(speedGraph, ref maxSpeed, ref lastUpdateTimestamp, 0, 20, 100, 999, TimeSpan.FromMilliseconds(200));
        log($"总字节数为 0 后：最大速度 = {maxSpeed}，最后更新时间戳 = {lastUpdateTimestamp}");
        await delayProvider(TimeSpan.FromMilliseconds(700));
        cancellationToken.ThrowIfCancellationRequested();

        long firstUpdateTimestamp = Stopwatch.Frequency;
        log("步骤 3：进度超过总量，百分比被限制在 100%，负速度会被归零");
        GamePackageOperationSpeedGraphHelper.UpdateSpeedGraph(speedGraph, ref maxSpeed, ref lastUpdateTimestamp, 1000, 1500, -15, firstUpdateTimestamp, TimeSpan.FromMilliseconds(200));
        log($"百分比已限制：最大速度 = {maxSpeed}，最后更新时间戳 = {lastUpdateTimestamp}");
        await delayProvider(TimeSpan.FromMilliseconds(700));
        cancellationToken.ThrowIfCancellationRequested();

        long secondUpdateTimestamp = firstUpdateTimestamp + (long)(Stopwatch.Frequency * 0.25);
        log("步骤 4：更高速度刷新最大值");
        GamePackageOperationSpeedGraphHelper.UpdateSpeedGraph(speedGraph, ref maxSpeed, ref lastUpdateTimestamp, 1000, 250, 300, secondUpdateTimestamp, TimeSpan.FromMilliseconds(200));
        log($"最大速度更新为 {maxSpeed}");
        await delayProvider(TimeSpan.FromMilliseconds(700));
        cancellationToken.ThrowIfCancellationRequested();

        long skippedUpdateTimestamp = secondUpdateTimestamp + (long)(Stopwatch.Frequency * 0.1);
        log("步骤 5：间隔不足，跳过刷新");
        GamePackageOperationSpeedGraphHelper.UpdateSpeedGraph(speedGraph, ref maxSpeed, ref lastUpdateTimestamp, 1000, 250, 300, skippedUpdateTimestamp, TimeSpan.FromMilliseconds(200));
        log($"跳过刷新后：最大速度 = {maxSpeed}，最后更新时间戳 = {lastUpdateTimestamp}");
    }
}
