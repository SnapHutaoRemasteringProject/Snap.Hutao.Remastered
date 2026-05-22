// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Git;

public sealed class MirrorRuntimeStats
{
    public MirrorRuntimeStats(string url, int serverPriority = 20)
    {
        Url = url;
        ServerPriority = serverPriority;
    }

    public string Url { get; init; }

    // Average throughput in Mbps (EMA)
    public double AvgThroughputMbps { get; set; }

    // First packet latency in ms
    public double AvgFirstPacketMs { get; set; }

    // Connect latency in ms
    public double AvgConnectMs { get; set; }

    // Time to first byte in ms
    public double AvgTTFBMs { get; set; }

    // ls-remote latency in ms
    public double AvgLsRemoteMs { get; set; }

    // Total Failure Rate
    public double FailureRate { get; set; }

    // Consecutive Failures
    public int ConsecutiveFailures { get; set; }

    // Circuit Breaker state
    public bool IsCircuitBroken { get; set; }

    // When to recover from Circuit Breaker
    public DateTimeOffset CircuitBrokenUntilUtc { get; set; }

    // Network environment representation
    public string NetworkFingerprint { get; set; } = string.Empty;

    // Server-side priority
    public int ServerPriority { get; set; }
}
