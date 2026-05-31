// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Git;

[Service(ServiceLifetime.Singleton, typeof(IMirrorScheduler))]
public sealed partial class MirrorScheduler : IMirrorScheduler
{
    /// <summary>
    /// 基于 mirrorIdentifier 的运行时统计数据存储。
    /// mirrorIdentifier 为 FriendlyName 或域名，避免重复存储相同镜像源。
    /// </summary>
    private readonly ConcurrentDictionary<string, MirrorRuntimeStats> statsMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 根据镜像源标识符列表返回排序后的镜像源列表。
    /// 排序基于多维评分算法，考虑吞吐量、延迟、故障率和熔断状态。
    /// </summary>
    /// <param name="mirrorIdentifiers">待排序的镜像源标识符列表</param>
    /// <returns>按评分从高到低排序的镜像源标识符列表</returns>
    public IReadOnlyList<string> GetSortedMirrors(IEnumerable<string> mirrorIdentifiers)
    {
        List<(string Identifier, double Score)> scoredMirrors = new();

        foreach (string identifier in mirrorIdentifiers)
        {
            MirrorRuntimeStats stats = GetOrAddStats(identifier);

            (string Identifier, double Score) scoredMirror = CreateScoredMirror(identifier, stats);
            scoredMirrors.Add(scoredMirror);
        }

        // 按评分从高到低排序，返回排序后的标识符列表
        return scoredMirrors
            .OrderByDescending(x => x.Score)
            .Select(x => x.Identifier)
            .ToList();
    }

    public MirrorRuntimeStats? GetRuntimeStats(string mirrorIdentifier)
    {
        return statsMap.TryGetValue(mirrorIdentifier, out MirrorRuntimeStats? stats) ? stats : null;
    }

    public void ReportThroughput(string mirrorIdentifier, double mbps)
    {
        MirrorRuntimeStats stats = GetOrAddStats(mirrorIdentifier);
        // Exponential Moving Average
        stats.AvgThroughputMbps = (stats.AvgThroughputMbps * 0.7) + (mbps * 0.3);
    }

    public void ReportFirstPacketLatency(string mirrorIdentifier, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(mirrorIdentifier);
        if (stats.AvgFirstPacketMs <= 0)
        {
            stats.AvgFirstPacketMs = latencyMs;
        }
        else
        {
            stats.AvgFirstPacketMs = (stats.AvgFirstPacketMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportConnectLatency(string mirrorIdentifier, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(mirrorIdentifier);
        if (stats.AvgConnectMs <= 0)
        {
            stats.AvgConnectMs = latencyMs;
        }
        else
        {
            stats.AvgConnectMs = (stats.AvgConnectMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportTTFB(string mirrorIdentifier, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(mirrorIdentifier);
        if (stats.AvgTTFBMs <= 0)
        {
            stats.AvgTTFBMs = latencyMs;
        }
        else
        {
            stats.AvgTTFBMs = (stats.AvgTTFBMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportLsRemoteLatency(string mirrorIdentifier, long latencyMs)
    {
        MirrorRuntimeStats stats = GetOrAddStats(mirrorIdentifier);
        if (stats.AvgLsRemoteMs <= 0)
        {
            stats.AvgLsRemoteMs = latencyMs;
        }
        else
        {
            stats.AvgLsRemoteMs = (stats.AvgLsRemoteMs * 0.7) + (latencyMs * 0.3);
        }
    }

    public void ReportSuccess(string mirrorIdentifier)
    {
        MirrorRuntimeStats stats = GetOrAddStats(mirrorIdentifier);
        stats.ConsecutiveFailures = 0;
        stats.IsCircuitBroken = false;

        // Slightly decay the failure rate towards 0 when successful
        stats.FailureRate = stats.FailureRate * 0.8;
    }

    public void ReportFailure(string mirrorIdentifier)
    {
        MirrorRuntimeStats stats = GetOrAddStats(mirrorIdentifier);
        stats.ConsecutiveFailures++;
        stats.FailureRate = (stats.FailureRate * 0.8) + 0.2; // Increase failure rate softly

        if (stats.ConsecutiveFailures >= 3 && !stats.IsCircuitBroken)
        {
            stats.IsCircuitBroken = true;
            stats.CircuitBrokenUntilUtc = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30);
        }
    }

    private MirrorRuntimeStats GetOrAddStats(string mirrorIdentifier)
    {
        return statsMap.GetOrAdd(mirrorIdentifier, identifier => new MirrorRuntimeStats(identifier));
    }

    /// <summary>
    /// 检查并刷新 Circuit Breaker 状态。
    /// 如果熔断状态已过期，则恢复镜像源。
    /// </summary>
    private void RefreshCircuitBreakerState(MirrorRuntimeStats stats)
    {
        if (stats.IsCircuitBroken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now >= stats.CircuitBrokenUntilUtc)
            {
                stats.IsCircuitBroken = false;
                stats.ConsecutiveFailures = 0;
            }
        }
    }

    /// <summary>
    /// 为镜像源创建评分条目。
    /// 包含 Circuit Breaker 状态检查和评分计算。
    /// </summary>
    private (string Identifier, double Score) CreateScoredMirror(string identifier, MirrorRuntimeStats stats)
    {
        RefreshCircuitBreakerState(stats);
        double score = CalculateScore(stats);
        return (identifier, score);
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
