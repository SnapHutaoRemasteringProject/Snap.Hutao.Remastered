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

    /// <summary>
    /// 任务正常启动并完成，但刷新本身失败（网络、登录失效等）。
    /// 与 <see cref="Failed"/> 区分：脚本和计划任务都是好的，问题在刷新过程。
    /// </summary>
    RefreshFailed,

    /// <summary>
    /// 服务器在刷新时要求人机验证。验证只能在应用内的窗口完成，
    /// 后台进程无法通过，需要用户打开每日便笺页面手动刷新一次。
    /// </summary>
    VerificationRequired,

    /// <summary>其他失败（网络、AUMID 激活失败等），调用方应引导用户查看日志。</summary>
    Failed,
}