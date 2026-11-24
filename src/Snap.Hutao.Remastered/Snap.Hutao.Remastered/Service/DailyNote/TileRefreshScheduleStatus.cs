// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 后台刷新计划任务的运行状态。
/// </summary>
public enum TileRefreshScheduleStatus
{
    /// <summary>未注册（用户未开启，或当前不是 MSIX 安装）。</summary>
    NotRegistered,

    /// <summary>已注册但还没有运行记录。</summary>
    NotYetRun,

    /// <summary>最近一次运行成功。</summary>
    Succeeded,

    /// <summary>
    /// 找不到执行文件：包装脚本或随包脚本被删除，
    /// 常见于杀毒软件隔离。指标记为 <c>0x80070002</c>。
    /// </summary>
    ScriptMissing,

    /// <summary>
    /// 系统脚本宿主被禁用（组策略或安全软件），指标记为 <c>0x80041305</c>。
    /// </summary>
    ScriptHostDisabled,

    /// <summary>其他失败（网络、AUMID 激活失败等），调用方应引导用户查看日志。</summary>
    Failed,
}