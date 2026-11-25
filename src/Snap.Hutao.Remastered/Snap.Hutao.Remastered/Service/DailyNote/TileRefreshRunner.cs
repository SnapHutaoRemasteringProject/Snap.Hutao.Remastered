// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Immutable;
using WebDailyNote = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote.DailyNote;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 计划任务以包身份唤起应用时执行的无 UI 磁贴刷新。
/// 不复用 <see cref="IDailyNoteService.RefreshDailyNotesAsync"/>：那条路径会更新 UI 绑定的集合，
/// 需要 XAML 线程；这里只读数据库、拉接口、写回、推磁贴。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(ITileRefreshRunner))]
internal sealed partial class TileRefreshRunner : ITileRefreshRunner
{
    private readonly IDailyNoteTileService dailyNoteTileService;
    private readonly IDailyNoteRepository dailyNoteRepository;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<TileRefreshRunner> logger;

    [GeneratedConstructor]
    public partial TileRefreshRunner(IServiceProvider serviceProvider);

    public async ValueTask<TileRefreshResult> RunAsync(CancellationToken token = default)
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("daily_note.tile_refresh_headless", "daily_note_tile");

        // 未打包进程没有包身份，<c>CreateTileUpdaterForApplication</c> 一定失败，提前返回。
        if (RuntimeEnvironment.IsUnpackaged)
        {
            span.SetData("daily_note.tile_refresh.skipped_unpackaged", true);
            return new(TileRefreshOutcome.NotPackaged);
        }

        ImmutableArray<DailyNoteEntry> entries = dailyNoteRepository.GetDailyNoteEntryImmutableArrayIncludingUser();
        if (entries.IsDefaultOrEmpty)
        {
            span.SetData("daily_note.tile_refresh.entries", 0);
            return new(TileRefreshOutcome.NoAccounts);
        }

        span.SetData("daily_note.tile_refresh.entries", entries.Length);

        int refreshed = 0;
        bool verificationRequired = false;
        string? failure = null;
        using (IServiceScope scope = serviceScopeFactory.CreateScope())
        {
            foreach (DailyNoteEntry entry in entries)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    TileRefreshOutcome entryOutcome = await TryRefreshEntryAsync(scope.ServiceProvider, entry, token).ConfigureAwait(false);
                    if (entryOutcome is TileRefreshOutcome.Succeeded)
                    {
                        refreshed++;
                    }
                    else if (entryOutcome is TileRefreshOutcome.VerificationRequired)
                    {
                        verificationRequired = true;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // 单个账户失败（token 过期、网络异常）不应中断其他账户。
                    // 保留第一条原因（含堆栈）：计划任务的状态提示与排障都依赖它。
                    failure ??= $"{entry.Uid}: {ex}";
                    logger.LogWarning(ex, "Headless tile refresh failed for uid {Uid}", entry.Uid);
                }
            }
        }

        span.SetData("daily_note.tile_refresh.refreshed", refreshed);

        if (refreshed == 0)
        {
            return new(verificationRequired ? TileRefreshOutcome.VerificationRequired : TileRefreshOutcome.RefreshFailed, failure);
        }

        // 磁贴 XML 只读 entry 的 DailyNote，不触碰任何 UI 集合。
        TileUpdateResult published = dailyNoteTileService.UpdatePrimaryTile(entries);
        span.SetData("daily_note.tile_refresh.published", published.Outcome.ToString());
        return published.Succeeded
            ? new(TileRefreshOutcome.Succeeded)
            : new(TileRefreshOutcome.PublishFailed, published.Failure);
    }

    private async ValueTask<TileRefreshOutcome> TryRefreshEntryAsync(IServiceProvider scopeProvider, DailyNoteEntry entry, CancellationToken token)
    {
        IGameRecordClient gameRecordClient = scopeProvider
            .GetRequiredService<IOverseaSupportFactory<IGameRecordClient>>()
            .Create(entry.User.IsOversea);

        UserAndUid userAndUid = UserAndUid.From(entry.User, entry.Uid);

        // 与交互式刷新一致：先请求 index，再取便笺，否则接口会要求先建立会话。
        await gameRecordClient.GetPlayerInfoAsync(userAndUid, token).ConfigureAwait(false);

        Response<WebDailyNote> response = await gameRecordClient.GetDailyNoteAsync(userAndUid, token).ConfigureAwait(false);

        // 无 UI 进程没有 InfoBar 订阅者，验证失败也不该往消息总线里丢提示。
        if (!ResponseValidator.TryValidateWithoutUINotification(response, out WebDailyNote? dailyNote))
        {
            // 1034：服务器要求人机验证。验证窗口只能由用户在应用内完成，
            // 无 UI 进程到此为止，把原因带回去让界面提示用户手动刷新一次。
            return response is { ReturnCode: (int)KnownReturnCode.CODE1034 }
                ? TileRefreshOutcome.VerificationRequired
                : TileRefreshOutcome.RefreshFailed;
        }

        // Update 同时写入 RefreshTime；无 UI 时 OnPropertyChanged 没有订阅者，可安全调用。
        entry.Update(dailyNote);
        dailyNoteRepository.UpdateDailyNoteEntry(entry);
        return TileRefreshOutcome.Succeeded;
    }
}