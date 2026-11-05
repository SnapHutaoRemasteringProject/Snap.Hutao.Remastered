// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.IO.Http.Proxy;
using Snap.Hutao.Remastered.Core.Logging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 磁贴图片缓存：下载远程图标与名片图，裁剪到磁贴允许的尺寸与体积，落盘到 LocalFolder，
/// 并把缓存文件解析为磁贴可以引用的本地 URI。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IDailyNoteTileImageCache))]
internal sealed partial class DailyNoteTileImageCache : IDailyNoteTileImageCache
{
    /// <summary>
    /// 图标地址。这些常量同时是内容工厂构建图标行时使用的图标标识，
    /// 因此对同程序集可见：地址只在这一处定义，避免两处字面量漂移。
    /// </summary>
    internal const string IconResinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_106.png";
    internal const string IconTaskUrl = "https://api.snaphutaorp.org/static/raw/Mark/UI_MarkQuest_Events_Proce.png";
    internal const string IconHomeCoinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_204.png";
    internal const string IconExpeditionUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_105005.png";
    internal const string IconTransformerUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_220021.png";

    /// <summary>
    /// 名片图下载的最大尝试次数。
    /// </summary>
    private const int DownloadMaxAttempts = 3;

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

    private static readonly (string Url, string FileName)[] IconEntries =
    [
        (IconResinUrl, "resin.png"),
        (IconTaskUrl, "task.png"),
        (IconHomeCoinUrl, "homecoin.png"),
        (IconExpeditionUrl, "expedition.png"),
        (IconTransformerUrl, "transformer.png"),
    ];

    /// <summary>
    /// 磁贴图片先写 PNG；超过体积上限时改写成 JPEG，查找时按此顺序回退。
    /// </summary>
    private static readonly string[] BackgroundImageExtensions = [".png", ".jpg"];

    private readonly DailyNoteOptions options;

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
        byte[] bytes;

        try
        {
            bytes = await DownloadImageAsync(nameCardUri, token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 该角色此前下载过、且宽版与大版都还在缓存里时，直接沿用旧图。
            if (HasCachedBackground(avatarId))
            {
                SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateError("Reuse cached tile background", "DailyNoteTileImageCache", [("error", ex.GetType().Name), ("avatarId", avatarId)]));
                return false;
            }

            throw;
        }

        token.ThrowIfCancellationRequested();

        string directory = BackgroundCacheDirectory;
        Directory.CreateDirectory(directory);

        using (Image image = Image.FromStream(new MemoryStream(bytes)))
        {
            CropAndSave(image, Path.Combine(directory, $"{avatarId}_wide.png"), 31d / 15d);
            CropAndSave(image, Path.Combine(directory, $"{avatarId}_large.png"), 1d);
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
    /// 下载图片字节。
    /// <para>
    /// 与项目其余请求保持一致：显式使用系统代理（<see cref="HttpProxyUsingSystemProxy"/>），
    /// 并开启自动重定向——静态资源域名会把请求 302 到 <c>static.snaphutaorp.org</c>，
    /// 而项目共用的 <see cref="HttpClient"/> 关闭了重定向，直接用只会拿到 302 的响应体。
    /// </para>
    /// <para>
    /// 名片图约 600KB，经代理时可能有连接抖动，因此设置较短的连接超时并重试若干次，
    /// 避免一次失败就让用户看到「设置磁贴背景失败」。
    /// </para>
    /// </summary>
    private async Task<byte[]> DownloadImageAsync(Uri uri, CancellationToken token)
    {
        using (SocketsHttpHandler handler = new()
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            UseProxy = true,
            Proxy = HttpProxyUsingSystemProxy.Instance,
            ConnectTimeout = TimeSpan.FromSeconds(15),
        })
        using (HttpClient client = new(handler)
        {
            Timeout = TimeSpan.FromSeconds(60),
        })
        {
            Exception? lastError = null;

            for (int attempt = 1; attempt <= DownloadMaxAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    using (HttpResponseMessage response = await client.GetAsync(uri, token).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested)
                {
                    // 取消不是失败：只有真正的网络错误才重试，取消必须立刻冒泡出去。
                    lastError = ex;
                    if (attempt < DownloadMaxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), token).ConfigureAwait(false);
                    }
                }
            }

            throw new HttpRequestException(SH.FormatServiceDailyNoteTileImageDownloadFailed(uri), lastError);
        }
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
            SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateError("Prepare tile icons failed", "DailyNoteTileImageCache", [("error", ex.GetType().Name)]));
        }
    }

    private async Task PrepareIconsCoreAsync()
    {
        string directory = IconCacheDirectory;
        Directory.CreateDirectory(directory);

        await Task.WhenAll(IconEntries.Select(entry => DownloadAndTrimAsync(entry, directory))).ConfigureAwait(false);
    }

    private async Task DownloadAndTrimAsync((string Url, string FileName) entry, string directory)
    {
        string outputPath = Path.Combine(directory, entry.FileName);
        if (File.Exists(outputPath))
        {
            return;
        }

        // 图标是一次性缓存，与用户的背景选择无关，因此不参与取消。
        byte[] bytes = await DownloadImageAsync(new Uri(entry.Url), CancellationToken.None).ConfigureAwait(false);
        using (Bitmap bitmap = new(new MemoryStream(bytes)))
        {
            TrimAndCenter(bitmap, outputPath);
        }
    }

    private void TrimAndCenter(Bitmap source, string outputPath)
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
        foreach ((string entryUrl, string fileName) in IconEntries)
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
        if (string.IsNullOrEmpty(avatarId))
        {
            return null;
        }

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
