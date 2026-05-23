// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Git;

[Service(ServiceLifetime.Singleton, typeof(IMirrorScheduler))]
public sealed partial class MirrorScheduler : IMirrorScheduler
{
    private readonly ConcurrentDictionary<string, MirrorRuntimeStats> statsMap = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<GitRepository> GetSortedMirrors(ImmutableArray<GitRepository> mirrors)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<(GitRepository Repo, double Score)> scoredMirrors = new();

        foreach (GitRepository mirror in mirrors)
        {
            string url = mirror.HttpsUrl.OriginalString;
            MirrorRuntimeStats stats = GetOrAddStats(url);

            // Half-open check for Circuit Breaker
            if (stats.IsCircuitBroken && now >= stats.CircuitBrokenUntilUtc)
            {
                stats.IsCircuitBroken = false;
                stats.ConsecutiveFailures = 0; // reset
            }

            double score = CalculateScore(stats);
            scoredMirrors.Add((mirror, score));
        }

        return scoredMirrors.OrderByDescending(x => x.Score).Select(x => x.Repo).ToList();
    }

    public MirrorRuntimeStats? GetRuntimeStats(string url)
    {
        return statsMap.TryGetValue(url, out MirrorRuntimeStats? stats) ? stats : null;
    }

    public void ReportThroughput(string url, double mbps)
    {
        MirrorRuntimeStats stats = GetOrAddStats(url);
        // Exponential Moving Average
        stats.AvgThroughputMbps = (stats.AvgThroughputMbps * 0.7) + (mbps * 0.3);
    }

    public void ReportFirstPacketLatency(string url, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(url);
        if (stats.AvgFirstPacketMs <= 0)
        {
            stats.AvgFirstPacketMs = latencyMs;
        }
        else
        {
            stats.AvgFirstPacketMs = (stats.AvgFirstPacketMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportConnectLatency(string url, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(url);
        if (stats.AvgConnectMs <= 0)
        {
            stats.AvgConnectMs = latencyMs;
        }
        else
        {
            stats.AvgConnectMs = (stats.AvgConnectMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportTTFB(string url, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(url);
        if (stats.AvgTTFBMs <= 0)
        {
            stats.AvgTTFBMs = latencyMs;
        }
        else
        {
            stats.AvgTTFBMs = (stats.AvgTTFBMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportLsRemoteLatency(string url, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(url);
        if (stats.AvgLsRemoteMs <= 0)
        {
            stats.AvgLsRemoteMs = latencyMs;
        }
        else
        {
            stats.AvgLsRemoteMs = (stats.AvgLsRemoteMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportSuccess(string url)
    {
        MirrorRuntimeStats stats = GetOrAddStats(url);
        stats.ConsecutiveFailures = 0;
        stats.IsCircuitBroken = false;

        // Slightly decay the failure rate towards 0 when successful
        stats.FailureRate = stats.FailureRate * 0.8;
    }

    public void ReportFailure(string url)
    {
        MirrorRuntimeStats stats = GetOrAddStats(url);
        stats.ConsecutiveFailures++;
        stats.FailureRate = (stats.FailureRate * 0.8) + 0.2; // Increase failure rate softly

        if (stats.ConsecutiveFailures >= 3 && !stats.IsCircuitBroken)
        {
            stats.IsCircuitBroken = true;
            stats.CircuitBrokenUntilUtc = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30);
        }
    }

    private MirrorRuntimeStats GetOrAddStats(string url)
    {
        return statsMap.GetOrAdd(url, u => new MirrorRuntimeStats(u));
    }

    private double CalculateScore(MirrorRuntimeStats stats)
    {
        if (stats.IsCircuitBroken)
        {
            return -10000;
        }

        double score = stats.ServerPriority;

        // Throughput is strongly positive
        score += stats.AvgThroughputMbps * 40;

        // Penalize connection and latency characteristics (scale ms to seconds)
        score -= (stats.AvgConnectMs / 1000.0) * 2.0;
        score -= (stats.AvgFirstPacketMs / 1000.0) * 5.0;
        score -= (stats.AvgTTFBMs / 1000.0) * 3.0;
        score -= (stats.AvgLsRemoteMs / 1000.0) * 1.0;

        // Penalize failures
        score -= stats.ConsecutiveFailures * 20;
        score -= stats.FailureRate * 100;

        return score;
    }
}
