// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Model.Entity;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 每日便签主磁贴服务：按账户裁剪并缓存名片背景图、构建磁贴 XML、推送到开始菜单。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IDailyNoteTileService))]
internal sealed partial class DailyNoteTileService : IDailyNoteTileService
{
    private readonly IDailyNoteTileImageCache imageCache;
    private readonly IDailyNoteTileContentFactory contentFactory;
    private readonly DailyNoteOptions options;

    [GeneratedConstructor]
    public partial DailyNoteTileService(IServiceProvider serviceProvider);

    /// <summary>
    /// 为指定账户下载名片并裁剪为磁贴背景图（宽版 31:15 与大版 1:1），成功后写入背景设置。
    /// <para>
    /// <paramref name="token"/> 用于取消先发起但用户已经不想要的选择：
    /// 写入设置之前必须再次检查，否则晚完成的旧请求会把新选择覆盖掉。
    /// </para>
    /// <para>
    /// 下载失败但该角色已有完整旧缓存时沿用旧图，仍然写入设置并返回
    /// <see langword="true"/>——用户要的是「背景换成这个角色」，
    /// 用该角色上一张缓存图同样满足，不必因为一次网络抖动就失败。
    /// </para>
    /// </summary>
    public async ValueTask<bool> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri, CancellationToken token = default)
    {
        await imageCache.PrepareBackgroundAsync(avatarId, nameCardUri, token).ConfigureAwait(false);

        // 落盘可能耗时，写设置前必须最后确认一次；否则被取消的旧请求仍会覆盖新选择。
        token.ThrowIfCancellationRequested();

        options.TileBackgroundAvatarId.Value = avatarId;
        options.TileBackgroundUid.Value = uid;
        return true;
    }

    /// <summary>
    /// 清除磁贴背景设置。
    /// </summary>
    public void ClearBackground()
    {
        options.TileBackgroundAvatarId.Value = string.Empty;
        options.TileBackgroundUid.Value = string.Empty;
    }

    /// <summary>
    /// 更新主磁贴：图标缓存尽力而为地后台准备，任何一步失败都不影响调用方的数据库写入。
    /// </summary>
    public bool TryUpdatePrimaryTile(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        if (RuntimeEnvironment.IsUnpackaged)
        {
            return false;
        }

        // 提权进程无法更新 shell 通知（见 ToastNotificationService 的非提权辅助进程方案）。
        // 主路径下提权实例由 runas 启动、不带包标识，上面的 IsUnpackaged 已拦住；
        // 这里再显式挡一次，覆盖 "已打包但提权" 的边缘情况。
        // 用 Environment.IsPrivilegedProcess 而不是 HutaoRuntime.IsProcessElevated：
        // 后者还混入了 OverrideElevationRequirement 设置开关，普通打包进程打开该开关后
        // 会被误判为提权，导致磁贴无法更新或清除。
        if (Environment.IsPrivilegedProcess)
        {
            return false;
        }

        // 图标缓存是尽力而为的后台准备，失败已在 helper 内记录，不阻塞磁贴更新。
        imageCache.PrepareIconsIgnoringFailureAsync().SafeForget();

        // 磁贴更新只是锦上添花，任何一步失败都不应该让调用方的数据库写入回滚或抛出。
        try
        {
            string? tileXml = entries.Count == 0
                ? contentFactory.BuildBackgroundOnlyTileXml()
                : contentFactory.BuildTileXml(entries);

            TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();

            // 没有任何内容可发布时必须用 Clear()：发送 <tile/> 没有内容绑定，
            // Windows 不会清除上一条通知，旧内容会一直留在磁贴上。
            if (string.IsNullOrEmpty(tileXml))
            {
                updater.Clear();
                return true;
            }

            XmlDocument document = new();
            document.LoadXml(tileXml);

            TileNotification notification = new(document);
            updater.Update(notification);
            return true;
        }
        catch (Exception ex)
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateError("Update primary tile failed", "DailyNoteTileService", [("error", ex.GetType().Name)]));
            return false;
        }
    }
}
