// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

internal interface ISpeedGraph
{
    void ResetGraph();

    void NormalGraph();

    void SetSpeed(double percent, ulong speed);
}

internal sealed class SpeedGraphAdapter(DevWinUI.SpeedGraph speedGraph) : ISpeedGraph
{
    public void ResetGraph()
    {
        speedGraph.ResetGraph();
    }

    public void NormalGraph()
    {
        speedGraph.NormalGraph();
    }

    public void SetSpeed(double percent, ulong speed)
    {
        speedGraph.SetSpeed(percent, speed);
    }
}

internal static class GamePackageOperationSpeedGraphHelper
{
    internal static void ResetSpeedGraph(ISpeedGraph speedGraph, ref ulong maxSpeed, ref long lastUpdateTimestamp)
    {
        speedGraph.ResetGraph();
        speedGraph.NormalGraph();
        speedGraph.SetSpeed(0, 0);
        maxSpeed = 1;
        lastUpdateTimestamp = 0;
    }

    internal static void UpdateSpeedGraph(ISpeedGraph speedGraph, ref ulong maxSpeed, ref long lastUpdateTimestamp, long totalBytes, long progressBytes, long speedBytesPerSecond, long currentTimestamp, TimeSpan updateInterval)
    {
        if (totalBytes <= 0)
        {
            ResetSpeedGraph(speedGraph, ref maxSpeed, ref lastUpdateTimestamp);
            return;
        }

        if (lastUpdateTimestamp is not 0 && System.Diagnostics.Stopwatch.GetElapsedTime(lastUpdateTimestamp, currentTimestamp) < updateInterval)
        {
            return;
        }

        lastUpdateTimestamp = currentTimestamp;
        ulong currentSpeed = speedBytesPerSecond < 0 ? 0UL : (ulong)speedBytesPerSecond;
        if (currentSpeed > maxSpeed)
        {
            maxSpeed = currentSpeed;
        }

        double percent = Math.Clamp((double)progressBytes / totalBytes * 100D, 0D, 100D);
        speedGraph.SetSpeed(percent, currentSpeed);
    }
}
