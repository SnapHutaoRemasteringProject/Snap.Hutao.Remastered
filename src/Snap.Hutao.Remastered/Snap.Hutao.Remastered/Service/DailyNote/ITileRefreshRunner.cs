// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 无 UI 的磁贴刷新：由系统计划任务以包身份唤起应用时调用，
/// 拉取真实便笺数据并推送磁贴，全程不启动 XAML。
/// </summary>
public interface ITileRefreshRunner
{
    /// <summary>
    /// 刷新所有已保存账户的便笺并更新磁贴。
    /// </summary>
    /// <returns>至少有一个账户刷新成功并发布了磁贴时为 <see langword="true"/>。</returns>
    ValueTask<bool> RunAsync(CancellationToken token = default);
}