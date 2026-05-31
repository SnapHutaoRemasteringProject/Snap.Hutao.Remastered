// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Git;

/// <summary>
/// 镜像源调度器接口。
/// 用于管理镜像源的性能统计、排序和故障处理。
/// 所有标识符 (mirrorIdentifier) 应为 FriendlyName 或域名，避免重复存储相同镜像源的多个副本。
/// </summary>
public interface IMirrorScheduler
{
    /// <summary>
    /// 根据镜像源标识符列表返回排序后的镜像源列表。
    /// 排序基于性能指标和故障状态。
    /// </summary>
    /// <param name="mirrorIdentifiers">待排序的镜像源标识符列表（FriendlyName 或域名）</param>
    /// <returns>按评分从高到低排序的镜像源标识符列表</returns>
    IReadOnlyList<string> GetSortedMirrors(IEnumerable<string> mirrorIdentifiers);

    /// <summary>
    /// 获取指定镜像源的运行时统计数据。
    /// </summary>
    /// <param name="mirrorIdentifier">镜像源标识符</param>
    /// <returns>运行时统计数据，若不存在则返回 null</returns>
    MirrorRuntimeStats? GetRuntimeStats(string mirrorIdentifier);

    /// <summary>
    /// 报告吞吐量（下载速度）。
    /// </summary>
    void ReportThroughput(string mirrorIdentifier, double mbps);

    /// <summary>
    /// 报告首包延迟。
    /// </summary>
    void ReportFirstPacketLatency(string mirrorIdentifier, long latencyMs);

    /// <summary>
    /// 报告连接延迟（DNS + TCP + TLS）。
    /// </summary>
    void ReportConnectLatency(string mirrorIdentifier, long latencyMs);

    /// <summary>
    /// 报告首字节时间 (TTFB)。
    /// </summary>
    void ReportTTFB(string mirrorIdentifier, long latencyMs);

    /// <summary>
    /// 报告 ls-remote 操作延迟。
    /// </summary>
    void ReportLsRemoteLatency(string mirrorIdentifier, long latencyMs);

    /// <summary>
    /// 报告镜像源操作成功。
    /// </summary>
    void ReportSuccess(string mirrorIdentifier);

    /// <summary>
    /// 报告镜像源操作失败。
    /// </summary>
    void ReportFailure(string mirrorIdentifier);
}
