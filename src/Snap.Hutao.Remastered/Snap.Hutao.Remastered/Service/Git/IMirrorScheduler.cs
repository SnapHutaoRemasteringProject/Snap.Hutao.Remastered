// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Git;

public interface IMirrorScheduler
{
    IReadOnlyList<GitRepository> GetSortedMirrors(ImmutableArray<GitRepository> mirrors);

    void ReportThroughput(string url, double mbps);

    void ReportFirstPacketLatency(string url, long latencyMs);

    void ReportConnectLatency(string url, long latencyMs);

    void ReportTTFB(string url, long latencyMs);

    void ReportLsRemoteLatency(string url, long latencyMs);

    void ReportSuccess(string url);

    void ReportFailure(string url);
}
