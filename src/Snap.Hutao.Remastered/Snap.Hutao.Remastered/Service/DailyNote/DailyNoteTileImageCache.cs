// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
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
    /// <summary>
    /// 图标地址与缓存文件名，以及逐个图标的水平视觉校准量（画布像素，正 = 右移）。
    /// <para>
    /// 这些常量同时是内容工厂构建图标行时使用的图标标识，
    /// 因此对同程序集可见：地址只在这一处定义，避免两处字面量漂移。
    /// </para>
    /// <para>
    /// 校准量的由来：自动居中用的是不透明度加权的质心，但人眼判断「对齐」时
    /// 对轮廓和色彩重量更敏感——弯月是半透明浅色、四角星与瓶子是高饱和不透明，
    /// 质心对齐后视觉上仍会显得下面两个偏左。因此保留一张人工标定的微调表，
    /// 数字由实际渲染效果反推，0 表示无需额外补偿。
    /// </para>
    /// </summary>
    internal const string IconResinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_106.png";
    internal const string IconTaskUrl = "https://api.snaphutaorp.org/static/raw/Mark/UI_MarkQuest_Events_Proce.png";
    internal const string IconHomeCoinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_204.png";
    internal const string IconExpeditionUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_105005.png";
    internal const string IconTransformerUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_220021.png";

    /// <summary>
    /// 单次下载的总体超时（秒）。项目下载器（
    /// <see cref="Snap.Hutao.Remastered.Core.Caching.ImageCacheDownloadOperation"/>）使用
    /// 无限超时的 HttpClient，磁贴下载是用户交互的一部分，必须有上限；
    /// 因此在调用处用 <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/>
    /// 把超时作为取消令牌传进下载器——取消真的会中止请求，而不是放弃等待。
    /// 名片图约 600 KB，正常网络 3 秒内完成，留足慢网与管道重试余量。
    /// </summary>
    private const int DownloadTimeoutSeconds = 90;

    /// <summary>
    /// 临时下载文件的最短保留时间（分钟）。比在途下载的存活时间长得多，
    /// 因此清理时不会误删正在写入的文件，只回收异常退出留下的孤儿。
    /// </summary>
    private const int TemporaryFileRetentionMinutes = 10;

    /// <summary>
    /// 磁贴通知的图片上限：不得超过 1024x1024 像素，且不超过 200 KB；
    /// 任一条件不满足时 Windows 会直接丢弃整条通知。
    /// </summary>
    private const int MaxTileImagePixelSize = 1024;
    private const int MaxTileImageBytes = 200 * 1024;

    /// <summary>
    /// 磁贴图标在行内的实际显示尺寸很小（约 50px），源图保留到 128px 足够清晰，
    /// 再大只会白占缓存空间。
    /// </summary>
    private const int MaxTileIconPixelSize = 128;

    /// <summary>
    /// 图标缓存条目：地址、缓存文件名，以及人工标定的水平视觉校准量（画布像素，正 = 右移）。
    /// <para>
    /// 自动居中用的是不透明度加权的质心，但人眼判断「对齐」时对轮廓与色彩重量更敏感：
    /// 弯月是半透明浅色、四角星与瓶子是高饱和不透明，质心对齐后视觉上仍会显得偏。
    /// 因此保留一张人工标定表，数字按实际渲染效果反推，0 表示无需额外补偿。
    /// </para>
    /// </summary>
    private static readonly (string Url, string FileName, int HorizontalNudge)[] IconEntries =
    [
        (IconResinUrl, "resin.png", 0),
        (IconTaskUrl, "task.png", 5),
        (IconHomeCoinUrl, "homecoin.png", 0),
        (IconExpeditionUrl, "expedition.png", 5),
        (IconTransformerUrl, "transformer.png", 0),
    ];

    /// <summary>
    /// 磁贴图片先写 PNG；超过体积上限时改写成 JPEG，查找时按此顺序回退。
    /// </summary>
    private static readonly string[] BackgroundImageExtensions = [".png", ".jpg"];

    private readonly DailyNoteOptions options;
    private readonly IImageCacheDownloadOperation downloadOperation;
    private readonly ILogger<DailyNoteTileImageCache> logger;

    /// <summary>
    /// 图标缓存只准备一次；失败时保留 faulted 的任务状态，下一次调用重新尝试。
    /// </summary>
    private Task? iconsPreparedTask;

    [GeneratedConstructor]
    public partial DailyNoteTileImageCache(IServiceProvider serviceProvider);

    /// <summary>
    /// 磁贴通知的图片只能通过 ms-appx:///、ms-appdata:///local/ 或 http(s):// 引用，
    /// 没有 localcache 协议，因此缓存目录必须落在 LocalFolder 而不是 LocalCacheFolder。
    /// </summary>
    private string BackgroundCacheDirectory
    {
        get => Path.Combine(HutaoRuntime.LocalFolderDirectory, "TileBackgrounds");
    }

    private string IconCacheDirectory
    {
        get => Path.Combine(HutaoRuntime.LocalFolderDirectory, "TileIcons");
    }

    /// <summary>
    /// 为指定角色下载名片并裁剪为磁贴背景图（宽版 31:15 与大版 1:1）。
    /// <para>
    /// 只做下载、裁剪与落盘，不写设置项；写入设置由 <see cref="IDailyNoteTileService"/> 负责，
    /// 因此被取消的旧请求不会覆盖用户的新选择。
    /// </para>
    /// <para>
    /// 下载失败时如果该角色已有完整缓存，沿用旧图而不是抛出：
    /// 背景图地址在网络抖动时经常连不上，而缓存本身已经是一张可用的背景图，
    /// 沿用它可以避免「网络一抖就换不了背景」。用户主动取消不在兜底范围内。
    /// </para>
    /// </summary>
    /// <param name="avatarId">角色 ID，决定缓存文件名。</param>
    /// <param name="nameCardUri">名片图地址。</param>
    /// <param name="token">取消标记：用户改选后必须尽快中止下载与落盘。</param>
    public async ValueTask<bool> PrepareBackgroundAsync(string avatarId, Uri nameCardUri, CancellationToken token)
    {
        string directory = BackgroundCacheDirectory;
        Directory.CreateDirectory(directory);

        // 上一次异常退出可能留下临时文件，先回收陈旧的。
        DeleteStaleTemporaryFiles(directory);

        string temporaryFile = Path.Combine(directory, $"{avatarId}_{Guid.NewGuid():N}.download");

        try
        {
            // 下载交给项目的标准下载器：统一管道（系统代理、
            // ServerDomainRedirectHandler 处理 302、RetryHttpHandler 重试）。
            // 磁贴只负责下载后的裁剪与落盘，不再重实现下载逻辑。
            //
            // 超时必须作为令牌传进下载器，不能只用 WaitAsync：WaitAsync 只是停止等待，
            // 被放弃的请求会继续跑（占着响应体写入临时文件，也会占用共享客户端的连接）。
            using (CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeoutSource.CancelAfter(TimeSpan.FromSeconds(DownloadTimeoutSeconds));
                await downloadOperation.DownloadFileAsync(nameCardUri, temporaryFile, timeoutSource.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            TryDeleteFile(temporaryFile);

            // 该角色此前下载过、且宽版与大版都还在缓存里时，直接沿用旧图。
            // 注意不能按异常类型排除取消：WaitAsync 超时抛出的 TimeoutException
            // 不是 OperationCanceledException，而真正的用户取消 token 已被取消，
            // 以 token 是否被取消为准。
            if (HasCachedBackground(avatarId))
            {
                logger.LogWarning(ex, "Reuse cached tile background for avatar {AvatarId}", avatarId);
                return false;
            }

            throw;
        }

        token.ThrowIfCancellationRequested();

        try
        {
            // 先读进内存再解码：Image.FromFile / new Bitmap(path) 会一直持有文件句柄，
            // 之后的删除会因文件被占用而失败，临时文件就堆积下来了。
            byte[] bytes = await File.ReadAllBytesAsync(temporaryFile, token).ConfigureAwait(false);
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

        // 超限时 CropAndSave 会改写同名 JPEG，残留的 PNG 会让下一次解析错误命中旧图。
        foreach (string suffix in (string[])["_wide", "_large"])
        {
            string pngPath = Path.Combine(directory, $"{avatarId}{suffix}.png");
            string jpgPath = Path.Combine(directory, $"{avatarId}{suffix}.jpg");
            if (File.Exists(pngPath) && File.Exists(jpgPath))
            {
                File.Delete(pngPath);
            }
        }

        return true;
    }

    /// <summary>
    /// 尽力删除临时文件。删除失败不中断调用方，但必须留下记录，
    /// 否则残留文件无从追查（仓库约定：catch 必须记录、上报或重抛）。
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
    /// 判断该角色的背景图缓存是否完整可用：宽版与大版都必须存在。
    /// <para>
    /// 只存在一张时不能沿用——磁贴的宽磁贴与大磁贴分别引用 <c>_wide</c> 与 <c>_large</c>，
    /// 缺任何一张都会让对应尺寸回退成没有背景。
    /// </para>
    /// </summary>
    private bool HasCachedBackground(string avatarId)
    {
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

        // 先按原尺寸裁剪，再等比缩放到最长边不超过 1024 像素。
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
    /// 逐级降低质量重试，直到 PNG 体积落在磁贴允许的 200 KB 以内。
    /// </summary>
    private void SaveCompressedPng(Bitmap output, string path)
    {
        output.Save(path, ImageFormat.Png);
        if (new FileInfo(path).Length <= MaxTileImageBytes)
        {
            return;
        }

        // PNG 无法调质量，超过上限时改用 JPEG 逐级压缩。
        string jpegPath = Path.ChangeExtension(path, ".jpg");
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

        // JPEG 成功落盘后删除超限的 PNG，否则解析会优先命中那张被 Windows 丢弃的图。
        if (File.Exists(jpegPath) && new FileInfo(jpegPath).Length <= MaxTileImageBytes)
        {
            File.Delete(path);
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
    /// <para>
    /// 图标下载失败只会让磁贴回退到远程 URL，不影响便笺内容本身，属于可处理的失败；
    /// 因此在这里记 breadcrumb 而不是交给 <c>SafeForget</c> 把它推进全局异常池，
    /// 那个机制只允许用在生命周期边界上。
    /// </para>
    /// <para>
    /// 失败不会缓存：<see cref="PrepareIconsAsync"/> 保留了 faulted 的任务状态，
    /// 下一次磁贴更新会重新尝试下载。
    /// </para>
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
        string directory = IconCacheDirectory;
        Directory.CreateDirectory(directory);

        // 回收上一次异常退出留下的临时文件。
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

        // 图标是一次性缓存，与用户的背景选择无关，因此不参与取消。
        // 下载交给项目的标准下载器（统一管道），磁贴只做裁剪。
        string temporaryFile = $"{outputPath}.{Guid.NewGuid():N}.download";
        try
        {
            // 图标下载同样把超时作为令牌传进下载器，避免放弃等待后请求仍在后台跑。
            using (CancellationTokenSource timeoutSource = new(TimeSpan.FromSeconds(DownloadTimeoutSeconds)))
            {
                await downloadOperation.DownloadFileAsync(new Uri(entry.Url), temporaryFile, timeoutSource.Token).ConfigureAwait(false);
            }

            // 先读进内存再解码，避免 GDI+ 持有文件句柄导致临时文件删不掉。
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
    /// 回收陈旧的临时下载文件。只清理超过 <see cref="TemporaryFileRetentionMinutes"/>
    /// 分钟没有变动的文件：在途下载最多只活 <see cref="DownloadTimeoutSeconds"/> 秒，
    /// 因此不会误删正在写入的文件；异常退出留下的孤儿文件则能在这里被收掉。
    /// 目录枚举失败只记录，不影响本次下载。
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

        // 统一画布与内容尺寸：源图有的 80x80、有的 256x256，裁剪后内容宽高比也各不相同。
        // ScaleToFit 只缩不放，小源图会得到小画布，磁贴里各图标的显示尺寸于是不一致，
        // 看起来就像没对齐。这里一律等比缩放到 128 满边，画布固定 128x128。
        double iconScale = (double)MaxTileIconPixelSize / Math.Max(contentW, contentH);
        int drawW = Math.Max(1, (int)Math.Round(contentW * iconScale));
        int drawH = Math.Max(1, (int)Math.Round(contentH * iconScale));
        int size = MaxTileIconPixelSize;

        double drawnX = (size - drawW) / 2d;
        double drawnY = (size - drawH) / 2d;

        // 按「视觉重心」居中，而不是「边框重心」：探索派遣等图标图形左重右轻，
        // 边框居中后视觉质心偏左，看起来就跟上面两个图标没对齐。
        // 这里用不透明度加权的水平质心反向补偿绘制位置，让视觉重心落在画布水平中心。
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

            // 质心相对内容区中心的偏移（源图像素，正 = 偏右）。质心偏左时让绘制位置右移。
            double offsetX = centroidX - contentCenterX;
            double scaleX = (double)drawW / contentW;
            drawnX -= offsetX * scaleX;
        }

        // 人工标定的视觉校准量：质心对齐后，轮廓与色彩重量不同的图标看起来仍会有偏差。
        drawnX += horizontalNudge;

        // 补偿可能让内容略微越出画布，磁贴的圆形裁剪本来就会吃掉圆外部分，
        // 因此这里只做宽松保护，不再硬拉回画布内——拉回去等于补偿不足，图标依旧偏。
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
        foreach ((string entryUrl, string fileName, _) in IconEntries)
        {
            if (entryUrl == url)
            {
                string path = Path.Combine(IconCacheDirectory, fileName);
                return File.Exists(path)
                    ? $"ms-appdata:///local/TileIcons/{fileName}"
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
                return $"ms-appdata:///local/TileBackgrounds/{fileName}";
            }
        }

        return null;
    }
}
