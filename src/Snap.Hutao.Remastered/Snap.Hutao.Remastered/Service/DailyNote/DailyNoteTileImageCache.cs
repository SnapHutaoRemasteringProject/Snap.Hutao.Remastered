// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 磁贴图片缓存：下载远程图标与名片图，裁剪到磁贴允许的尺寸与体积，落盘到 LocalFolder，
/// 并把缓存文件解析为磁贴可以引用的本地 URI。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IDailyNoteTileImageCache))]
internal sealed partial class DailyNoteTileImageCache : IDailyNoteTileImageCache
{
    // 图标地址同时是内容工厂构建图标行时使用的图标标识，只在这一处定义避免两处字面量漂移。
    internal const string IconResinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_106.png";
    internal const string IconTaskUrl = "https://api.snaphutaorp.org/static/raw/Mark/UI_MarkQuest_Events_Proce.png";
    internal const string IconHomeCoinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_204.png";
    internal const string IconExpeditionUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_105005.png";
    internal const string IconTransformerUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_220021.png";

    /// <summary>
    /// 单次下载的总体超时（秒）。项目下载器使用无限超时的 HttpClient，磁贴下载是用户交互的一部分必须有上限，
    /// 因此把超时作为取消令牌传进下载器——取消真的会中止请求，而不是放弃等待。
    /// </summary>
    private const int DownloadTimeoutSeconds = 90;

    /// <summary>
    /// 临时下载文件的最短保留时间（分钟）。比在途下载的存活时间长得多，
    /// 清理时不会误删正在写入的文件，只回收异常退出留下的孤儿。
    /// </summary>
    private const int TemporaryFileRetentionMinutes = 10;

    // 磁贴通知的图片上限：任一条件不满足时 Windows 会直接丢弃整条通知。
    private const int MaxTileImagePixelSize = 1024;
    private const int MaxTileImageBytes = 200 * 1024;

    // 磁贴图标在行内的实际显示尺寸很小，源图保留到 128px 足够清晰。
    private const int MaxTileIconPixelSize = 128;

    /// <summary>
    /// 图标缓存条目：地址、缓存文件名，以及人工标定的水平视觉校准量（画布像素，正 = 右移）。
    /// 自动居中用的是不透明度加权的质心，但人眼对轮廓与色彩重量更敏感，质心对齐后
    /// 视觉上仍会显得偏；校准量按实际渲染效果反推，0 表示无需额外补偿。
    /// </summary>
    private static readonly (string Url, string FileName, int HorizontalNudge)[] IconEntries =
    [
        (IconResinUrl, "resin.png", 0),
        (IconTaskUrl, "task.png", 5),
        (IconHomeCoinUrl, "homecoin.png", 0),
        (IconExpeditionUrl, "expedition.png", 5),
        (IconTransformerUrl, "transformer.png", 0),
    ];

    // 磁贴图片先写 PNG；超过体积上限时改写成 JPEG，查找时按此顺序回退。
    private static readonly string[] BackgroundImageExtensions = [".png", ".jpg"];

    private readonly DailyNoteOptions options;
    private readonly IImageCacheDownloadOperation downloadOperation;
    private readonly ILogger<DailyNoteTileImageCache> logger;

    /// <summary>
    /// 图标缓存只准备一次；失败时保留 faulted 的任务状态，下一次调用重新尝试。
    /// </summary>
    private Task? iconsPreparedTask;

    /// <summary>
    /// 旧目录迁移只尝试一次，避免每次判断缓存都去碰文件系统。
    /// </summary>
    private bool legacyMigrationDone;

    [GeneratedConstructor]
    public partial DailyNoteTileImageCache(IServiceProvider serviceProvider);

    // 背景图与程序图片缓存放在同一个 LocalCache 目录下：磁贴用 ms-appdata:///localcache/
    // 引用它们。官方 schema 只列出 ms-appx / ms-appdata:///local / http(s) 三种，
    // 但 localcache 与裸 Windows 路径实测同样能被渲染（Windows 11 22631，2026-11 验证），
    // 这样就不必再单独维护一份 TileBackgrounds 副本。
    private string BackgroundCacheDirectory
    {
        get => Path.Combine(HutaoRuntime.LocalCacheDirectory, "TileBackgrounds");
    }

    private string IconCacheDirectory
    {
        get => Path.Combine(HutaoRuntime.LocalCacheDirectory, "TileIcons");
    }

    /// <summary>
    /// 为指定角色准备磁贴背景图（宽版 31:15 与大版 1:1）。
    /// 同一角色、同一张名片图已有完整缓存时直接复用，不访问网络；缓存缺失或名片图地址变了才下载。
    /// 下载失败但仍有旧缓存时沿用旧图。只做文件工作，不写设置项。
    /// </summary>
    public async ValueTask<TileBackgroundImageOutcome> PrepareBackgroundAsync(string avatarId, Uri nameCardUri, CancellationToken token)
    {
        // 服务层入口已有 daily_note_tile.prepare_background 事务时，这里作为子 span。
        using SentrySpanScope span = SentrySpanScope.StartOperation("daily_note_tile.prepare_background_image", "daily_note_tile");
        span.SetData("daily_note_tile.avatar_id", avatarId);

        string directory = BackgroundCacheDirectory;

        // 选择背景不需要联网：同一张名片图裁剪出来的结果是一样的，直接用磁盘上的裁剪结果。
        // 没有 .src 旁文件的缓存（升级前生成的）同样可用——它本来就是这个角色的图；
        // 只有记录了来源且来源已变（名片图换过）时才重新下载。
        if (HasCachedBackground(avatarId) && !await IsSourceChangedAsync(avatarId, nameCardUri, token).ConfigureAwait(false))
        {
            span.SetData("daily_note_tile.reused_cache", true);
            // 补写来源，下次判断能走精确路径。
            await WriteBackgroundSourceAsync(avatarId, nameCardUri, token).ConfigureAwait(false);
            return TileBackgroundImageOutcome.Cached;
        }
        Directory.CreateDirectory(directory);
        DeleteStaleTemporaryFiles(directory);

        string temporaryFile = Path.Combine(directory, $"{avatarId}_{Guid.NewGuid():N}.download");

        try
        {
            await PrepareSourceImageAsync(nameCardUri, temporaryFile, token).ConfigureAwait(false);
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            TryDeleteFile(temporaryFile);

            if (HasCachedBackground(avatarId))
            {
                logger.LogWarning(ex, "Reuse cached tile background for avatar {AvatarId}", avatarId);
                return TileBackgroundImageOutcome.ReusedAfterFailure;
            }

            throw;
        }

        token.ThrowIfCancellationRequested();

        try
        {
            // 先读进内存再解码：GDI+ 从文件路径解码会持有文件句柄，导致临时文件删不掉。
            byte[] bytes = await File.ReadAllBytesAsync(temporaryFile, token).ConfigureAwait(false);

            // 裁剪是「宽版 + 大版」两次独立写盘，中间失败会留下 A/B 混合的缓存。
            // 先删掉来源记录：之后若替换中断，.src 缺失会让下一次请求走下载路径修复，
            // 而不是误把混合缓存当作「来源一致」直接复用。
            TryDeleteFile(BackgroundSourceFile(avatarId));

            using (MemoryStream stream = new(bytes))
            {
                using (Image image = Image.FromStream(stream))
                {
                    CropAndSave(image, Path.Combine(directory, $"{avatarId}_wide.png"), 31d / 15d);
                    CropAndSave(image, Path.Combine(directory, $"{avatarId}_large.png"), 1d);
                }
            }
        }
        finally
        {
            TryDeleteFile(temporaryFile);
        }

        // 两张裁剪图都落盘后才写来源记录：缓存与来源一起提交，替换才算完成。
        await WriteBackgroundSourceAsync(avatarId, nameCardUri, token).ConfigureAwait(false);
        return TileBackgroundImageOutcome.Downloaded;
    }

    /// <summary>
    /// 取一张可供裁剪的原始名片图并写到 <paramref name="destination"/>。
    /// 优先复用程序自己的图片缓存：同一张名片图在角色列表等处早已下载过，
    /// 直接拷贝本地文件即可，不必再走一次网络。缓存缺失时才真正下载。
    /// </summary>
    private async ValueTask PrepareSourceImageAsync(Uri nameCardUri, string destination, CancellationToken token)
    {
        // 图片缓存位于 LocalCacheFolder，磁贴引用不到它，但读取源文件不受此限制：
        // 裁剪结果会写到磁贴真正引用的位置。
        if (TryGetCachedImage(nameCardUri) is { } cached && File.Exists(cached))
        {
            try
            {
                await using FileStream source = File.OpenRead(cached);
                await using FileStream target = File.Create(destination);
                await source.CopyToAsync(target, token).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 缓存文件读不了（被占用、权限变化）：退回下载，不让选背景失败。
                logger.LogDebug(ex, "Unable to reuse cached name card image, falling back to download");
            }
        }

        // 超时必须作为令牌传进下载器：只做 WaitAsync 的话，被放弃的请求会继续跑，
        // 占着响应体写入临时文件，也占着共享客户端的连接。
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(DownloadTimeoutSeconds));
        await downloadOperation.DownloadFileAsync(nameCardUri, destination, timeoutSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// 尝试从程序图片缓存取这张名片图，只做本地文件查找，不触发下载。
    /// 这里不能用 <c>IImageCache.GetFileFromCacheAsync</c>：缓存未命中时它会自行发起
    /// 一次<em>没有取消令牌也没有超时</em>的下载，服务器卡住时整条选背景流程会无限挂起，
    /// 用户改选别也中止不了。下载统一交给下面的有界下载路径。
    /// </summary>
    private ValueFile? TryGetCachedImage(Uri nameCardUri)
    {
        try
        {
            // 与 ImageCache 相同的哈希命名规则，直接算路径即可。
            string path = ImageCacheFile.GetHashedFile(HutaoRuntime.GetLocalCacheImageCacheDirectory(), nameCardUri);
            return File.Exists(path) ? path : null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Image cache lookup failed for {Uri}", nameCardUri);
            return null;
        }
    }

    /// <summary>
    /// 记录裁剪结果来自哪张名片图，用于判断缓存是否仍然对应当前地址。
    /// </summary>
    private async ValueTask WriteBackgroundSourceAsync(string avatarId, Uri nameCardUri, CancellationToken token)
    {
        try
        {
            await File.WriteAllTextAsync(BackgroundSourceFile(avatarId), nameCardUri.OriginalString, token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Unable to record tile background source for avatar {AvatarId}", avatarId);
        }
    }

    /// <summary>
    /// 判断缓存对应的名片图是否已经变了。
    /// 没有 <c>.src</c>（升级前生成的缓存）时返回 <see langword="false"/>：那批缓存本来就是该角色的图，
    /// 按可用处理即可，不必为了补记录而重新下载。
    /// 但读取 <c>.src</c> 失败时返回 <see langword="true"/>：读失败意味着无法确认来源，
    /// 不能当作「来源一致」直接复用，必须走下载路径重新确认。
    /// </summary>
    private async ValueTask<bool> IsSourceChangedAsync(string avatarId, Uri nameCardUri, CancellationToken token)
    {
        string path = BackgroundSourceFile(avatarId);

        // 明确区分「文件不存在」与「读取失败」：前者是升级迁移场景，视为可用；
        // 后者是 IO 故障，视为来源未知，走下载。
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string recorded = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
            return !string.Equals(recorded.Trim(), nameCardUri.OriginalString, StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Unable to read tile background source for avatar {AvatarId}; treat as changed", avatarId);
            return true;
        }
    }

    private string BackgroundSourceFile(string avatarId)
    {
        return Path.Combine(BackgroundCacheDirectory, $"{avatarId}.src");
    }

    /// <summary>
    /// 尽力删除临时文件。删除失败不中断调用方，但必须留下记录（仓库约定：catch 必须记录、上报或重抛）。
    /// </summary>
    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Unable to delete temporary tile file '{Path}'", path);
        }
    }

    /// <summary>
    /// 把旧版本留在 LocalFolder\TileBackgrounds 的裁剪图搬到新的 LocalCache 位置。
    /// 只做一次：搬完后删掉旧目录，避免用户在磁盘上留两份。
    /// </summary>
    private void MigrateLegacyBackgrounds()
    {
        if (legacyMigrationDone)
        {
            return;
        }

        legacyMigrationDone = true;

        // 背景图与图标都从 LocalFolder 搬到了 LocalCache。
        MigrateLegacyDirectory("TileBackgrounds", BackgroundCacheDirectory);
        MigrateLegacyDirectory("TileIcons", IconCacheDirectory);
    }

    /// <summary>
    /// 把旧版本留在 <c>LocalFolder\&lt;name&gt;</c> 的磁贴资源搬到新目录，搬完删掉旧目录。
    /// </summary>
    private void MigrateLegacyDirectory(string name, string target)
    {
        string legacy = Path.Combine(HutaoRuntime.LocalFolderDirectory, name);
        if (!Directory.Exists(legacy))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(target);

            foreach (string file in Directory.EnumerateFiles(legacy))
            {
                string destination = Path.Combine(target, Path.GetFileName(file));
                // 新位置已有同名文件说明是更新的结果，不要用旧文件覆盖。
                if (!File.Exists(destination))
                {
                    File.Move(file, destination);
                }
            }

            Directory.Delete(legacy, recursive: true);
        }
        catch (Exception ex)
        {
            // 迁移失败不影响功能：下次启动会重试，最坏情况是重新生成一份资源。
            logger.LogWarning(ex, "Unable to migrate legacy tile resources from {Path}", legacy);
        }
    }

    /// <summary>
    /// 判断该角色的背景图缓存是否完整可用：宽版与大版都必须存在。
    /// </summary>
    private bool HasCachedBackground(string avatarId)
    {
        MigrateLegacyBackgrounds();

        string directory = BackgroundCacheDirectory;
        foreach (string suffix in (string[])["_wide", "_large"])
        {
            bool found = false;
            foreach (string extension in BackgroundImageExtensions)
            {
                if (File.Exists(Path.Combine(directory, $"{avatarId}{suffix}{extension}")))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 按目标宽高比居中裁剪图片并保存为 PNG，同时缩放到磁贴允许的尺寸与体积内。
    /// </summary>
    private void CropAndSave(Image image, string path, double targetRatio)
    {
        double sourceRatio = (double)image.Width / image.Height;
        int width;
        int height;
        if (sourceRatio > targetRatio)
        {
            height = image.Height;
            width = (int)Math.Round(height * targetRatio);
        }
        else
        {
            width = image.Width;
            height = (int)Math.Round(width / targetRatio);
        }

        int offsetX = (image.Width - width) / 2;
        int offsetY = (image.Height - height) / 2;

        (width, height) = ScaleToFit(width, height, MaxTileImagePixelSize);

        using (Bitmap output = new(width, height))
        {
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle(offsetX, offsetY, image.Width - offsetX * 2, image.Height - offsetY * 2), GraphicsUnit.Pixel);
            }

            SaveCompressedPng(output, path);
        }
    }

    private (int Width, int Height) ScaleToFit(int width, int height, int maxSize)
    {
        if (width <= maxSize && height <= maxSize)
        {
            return (width, height);
        }

        double scale = maxSize / (double)Math.Max(width, height);
        width = Math.Max(1, (int)Math.Round(width * scale));
        height = Math.Max(1, (int)Math.Round(height * scale));
        return (width, height);
    }

    /// <summary>
    /// 逐级降低质量重试，直到图片体积落在磁贴允许的 200 KB 以内；PNG 超限时改用 JPEG。
    /// 每次保存后删掉另一种格式，保证磁盘上只有本次保存的格式，
    /// 否则旧格式会被解析器优先命中，磁贴显示旧图。
    /// </summary>
    private void SaveCompressedPng(Bitmap output, string path)
    {
        output.Save(path, ImageFormat.Png);

        string jpegPath = Path.ChangeExtension(path, ".jpg");

        if (new FileInfo(path).Length <= MaxTileImageBytes)
        {
            // 新 PNG 已经合规：删掉可能存在的旧 JPEG，它才是该被丢弃的格式。
            TryDeleteFile(jpegPath);
            return;
        }

        for (int quality = 90; quality >= 40; quality -= 10)
        {
            using (Bitmap rgb = new(output.Width, output.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(rgb))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(output, 0, 0, output.Width, output.Height);
                }

                using (System.Drawing.Imaging.EncoderParameters parameters = new(1))
                {
                    parameters.Param[0] = new(System.Drawing.Imaging.Encoder.Quality, quality);

                    ImageCodecInfo? jpegCodec = ImageCodecInfo.GetImageEncoders()
                        .FirstOrDefault(static codec => codec.FormatID == ImageFormat.Jpeg.Guid);

                    if (jpegCodec is not null)
                    {
                        rgb.Save(jpegPath, jpegCodec, parameters);
                    }
                }
            }

            if (new FileInfo(jpegPath).Length <= MaxTileImageBytes)
            {
                break;
            }
        }

        // JPEG 成功落盘后删除超限的 PNG；JPEG 仍超限则也删掉，避免留下坏文件。
        if (File.Exists(jpegPath))
        {
            if (new FileInfo(jpegPath).Length <= MaxTileImageBytes)
            {
                File.Delete(path);
            }
            else
            {
                File.Delete(jpegPath);
            }
        }
    }

    /// <summary>
    /// 准备图标缓存（幂等）。失败时不缓存结果，以便下次调用重新尝试。
    /// </summary>
    private Task PrepareIconsAsync()
    {
        if (iconsPreparedTask is { IsFaulted: false, IsCanceled: false })
        {
            return iconsPreparedTask;
        }

        iconsPreparedTask = PrepareIconsCoreAsync();
        return iconsPreparedTask;
    }

    /// <summary>
    /// 在后台准备图标缓存，并就地处理失败。
    /// </summary>
    public async Task PrepareIconsIgnoringFailureAsync()
    {
        try
        {
            await PrepareIconsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Prepare tile icons failed");
        }
    }

    private async Task PrepareIconsCoreAsync()
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("daily_note_tile.prepare_icons", "daily_note_tile");

        // 图标从旧版 LocalFolder\TileIcons 搬过来，避免重复下载。
        MigrateLegacyBackgrounds();

        string directory = IconCacheDirectory;
        Directory.CreateDirectory(directory);

        DeleteStaleTemporaryFiles(directory);

        await Task.WhenAll(IconEntries.Select(entry => DownloadAndTrimAsync(entry, directory))).ConfigureAwait(false);
    }

    private async Task DownloadAndTrimAsync((string Url, string FileName, int HorizontalNudge) entry, string directory)
    {
        string outputPath = Path.Combine(directory, entry.FileName);
        if (File.Exists(outputPath))
        {
            return;
        }

        string temporaryFile = $"{outputPath}.{Guid.NewGuid():N}.download";
        try
        {
            using (CancellationTokenSource timeoutSource = new(TimeSpan.FromSeconds(DownloadTimeoutSeconds)))
            {
                await downloadOperation.DownloadFileAsync(new Uri(entry.Url), temporaryFile, timeoutSource.Token).ConfigureAwait(false);
            }

            byte[] bytes = await File.ReadAllBytesAsync(temporaryFile, CancellationToken.None).ConfigureAwait(false);
            using (MemoryStream stream = new(bytes))
            {
                using (Bitmap bitmap = new(stream))
                {
                    TrimAndCenter(bitmap, outputPath, entry.HorizontalNudge);
                }
            }
        }
        finally
        {
            TryDeleteFile(temporaryFile);
        }
    }

    /// <summary>
    /// 回收陈旧的临时下载文件：超过 <see cref="TemporaryFileRetentionMinutes"/> 分钟没有变动的
    /// 文件才会被清理，在途下载不会被误删。目录枚举失败只记录，不影响本次下载。
    /// </summary>
    private void DeleteStaleTemporaryFiles(string directory)
    {
        try
        {
            DateTime cutoff = DateTime.UtcNow.AddMinutes(-TemporaryFileRetentionMinutes);
            foreach (string path in Directory.EnumerateFiles(directory, "*.download"))
            {
                if (File.GetLastWriteTimeUtc(path) < cutoff)
                {
                    TryDeleteFile(path);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Unable to recycle stale tile downloads in '{Directory}'", directory);
        }
    }

    /// <summary>
    /// 按视觉重心把图标内容居中到 128x128 画布，并缩放到统一尺寸。
    /// </summary>
    /// <param name="source">源图标。</param>
    /// <param name="outputPath">输出路径。</param>
    /// <param name="horizontalNudge">该图标的水平视觉校准量（画布像素，正 = 右移）。</param>
    private void TrimAndCenter(Bitmap source, string outputPath, int horizontalNudge)
    {
        int width = source.Width;
        int height = source.Height;
        int minX = width, minY = height, maxX = -1, maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (source.GetPixel(x, y).A <= 0)
                {
                    continue;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY)
        {
            source.Save(outputPath, ImageFormat.Png);
            return;
        }

        int contentW = maxX - minX + 1;
        int contentH = maxY - minY + 1;

        // 统一缩放到 128 满边、画布固定 128x128，否则各图标显示尺寸不一致，看起来像没对齐。
        double iconScale = (double)MaxTileIconPixelSize / Math.Max(contentW, contentH);
        int drawW = Math.Max(1, (int)Math.Round(contentW * iconScale));
        int drawH = Math.Max(1, (int)Math.Round(contentH * iconScale));
        int size = MaxTileIconPixelSize;

        double drawnX = (size - drawW) / 2d;
        double drawnY = (size - drawH) / 2d;

        // 用不透明度加权的水平质心反向补偿绘制位置，让视觉重心落在画布水平中心。
        double centroidWeight = 0;
        double centroidXSum = 0;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int alpha = source.GetPixel(x, y).A;
                if (alpha <= 0)
                {
                    continue;
                }

                centroidWeight += alpha;
                centroidXSum += x * (double)alpha;
            }
        }

        if (centroidWeight > 0)
        {
            double centroidX = centroidXSum / centroidWeight;
            double contentCenterX = (minX + maxX) / 2d;

            double offsetX = centroidX - contentCenterX;
            double scaleX = (double)drawW / contentW;
            drawnX -= offsetX * scaleX;
        }

        drawnX += horizontalNudge;

        // 宽松保护：圆形裁剪本来就会吃掉圆外部分，硬拉回画布内等于补偿不足。
        double slack = size * 0.25;
        drawnX = Math.Clamp(drawnX, -slack, size - drawW + slack);
        drawnY = Math.Clamp(drawnY, 0, size - drawH);

        using (Bitmap output = new(size, size))
        {
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source,
                    new Rectangle((int)Math.Round(drawnX), (int)Math.Round(drawnY), drawW, drawH),
                    new Rectangle(minX, minY, contentW, contentH),
                    GraphicsUnit.Pixel);
            }

            output.Save(outputPath, ImageFormat.Png);
        }
    }

    /// <summary>
    /// 把图标的远程地址解析为本地缓存地址；缓存不存在时回退到原始远程地址。
    /// </summary>
    public string? ResolveIconSource(string url)
    {
        MigrateLegacyBackgrounds();

        foreach ((string entryUrl, string fileName, _) in IconEntries)
        {
            if (entryUrl == url)
            {
                string path = Path.Combine(IconCacheDirectory, fileName);
                return File.Exists(path)
                    ? $"ms-appdata:///localcache/TileIcons/{fileName}"
                    : url;
            }
        }

        return url;
    }

    /// <summary>
    /// 解析当前已应用背景图的本地地址，没有可用背景图时返回 <see langword="null"/>。
    /// </summary>
    public string? ResolveBackgroundImageSource(bool isWide)
    {
        string? avatarId = options.TileBackgroundAvatarId.Value;
        return string.IsNullOrEmpty(avatarId) ? null : ResolveBackgroundImageSource(avatarId, isWide);
    }

    /// <summary>
    /// 解析指定角色已缓存背景图的文件系统路径，没有缓存时返回 <see langword="null"/>。
    /// 与 <see cref="ResolveBackgroundImageSource(string, bool)"/> 的区别：这里给的是真实路径，
    /// 供 XAML 的 Image 直接加载；那边的 ms-appdata 形式只有磁贴解析器认识。
    /// </summary>
    public string? ResolveCachedBackgroundFilePath(string avatarId, bool isWide)
    {
        string? path = ResolveCachedBackgroundFile(avatarId, isWide);
        return path;
    }

    private string? ResolveCachedBackgroundFile(string avatarId, bool isWide)
    {
        MigrateLegacyBackgrounds();

        string suffix = isWide ? "_wide" : "_large";
        foreach (string extension in BackgroundImageExtensions)
        {
            string path = Path.Combine(BackgroundCacheDirectory, $"{avatarId}{suffix}{extension}");
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// 解析指定角色已缓存的背景图本地地址，没有缓存时返回 <see langword="null"/>。
    /// </summary>
    public string? ResolveBackgroundImageSource(string avatarId, bool isWide)
    {
        string suffix = isWide ? "_wide" : "_large";
        foreach (string extension in BackgroundImageExtensions)
        {
            string fileName = $"{avatarId}{suffix}{extension}";
            if (File.Exists(Path.Combine(BackgroundCacheDirectory, fileName)))
            {
                return $"ms-appdata:///localcache/TileBackgrounds/{fileName}";
            }
        }

        return null;
    }
}
