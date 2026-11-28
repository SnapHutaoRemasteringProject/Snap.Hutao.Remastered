// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 一次无 UI 刷新的结果。
/// 区分「环境不支持」「没有账户」和「刷新出错」，否则计划任务的退出码无法反映真实原因。
/// </summary>
public enum TileRefreshOutcome
{
    /// <summary>至少一个账户刷新成功，磁贴已更新。</summary>
    Succeeded,

    /// <summary>进程没有 MSIX 包身份，磁贴 API 不可用（未打包运行）。</summary>
    NotPackaged,

    /// <summary>数据库里没有已保存的每日便笺账户。</summary>
    NoAccounts,

    /// <summary>
    /// 服务器要求人机验证（retcode 1034）。验证必须由用户在应用内的 WebView 完成，
    /// 无 UI 的进程无法通过，因此这种情况要提示用户回到应用里刷新一次。
    /// </summary>
    VerificationRequired,

    /// <summary>所有账户都刷新失败（登录失效、网络异常等）。</summary>
    RefreshFailed,

    /// <summary>数据刷新成功但推送磁贴失败。</summary>
    PublishFailed,
}

/// <summary>
/// 刷新结果与失败原因。<see cref="Failure"/> 只用于诊断与日志，
/// 不进入本地化文案，避免把内部异常文本显示给用户。
/// </summary>
/// <param name="Outcome">本次刷新的结果分类。</param>
/// <param name="Failure">第一个失败账户的错误摘要；成功时为 <see langword="null"/>。</param>
public sealed record TileRefreshResult(TileRefreshOutcome Outcome, string? Failure = null);