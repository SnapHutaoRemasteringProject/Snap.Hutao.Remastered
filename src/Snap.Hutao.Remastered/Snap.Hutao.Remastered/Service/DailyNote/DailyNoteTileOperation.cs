// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.IO.Http.Proxy;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Security;
using System.Text;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;
using WebDailyNote = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote.DailyNote;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 每日便签主磁贴更新：下载并缓存图标、按账户裁剪名刺背景图、构建磁贴 XML 并推送到开始菜单。
/// </summary>
[Service(ServiceLifetime.Singleton)]
internal sealed partial class DailyNoteTileOperation
{
    private const string IconResinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_106.png";
    private const string IconTaskUrl = "https://api.snaphutaorp.org/static/raw/Mark/UI_MarkQuest_Events_Proce.png";
    private const string IconHomeCoinUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_204.png";
    private const string IconExpeditionUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_105005.png";
    private const string IconTransformerUrl = "https://api.snaphutaorp.org/static/raw/ItemIcon/UI_ItemIcon_220021.png";

    private static readonly (string Url, string FileName)[] IconEntries =
    [
        (IconResinUrl, "resin.png"),
        (IconTaskUrl, "task.png"),
        (IconHomeCoinUrl, "homecoin.png"),
        (IconExpeditionUrl, "expedition.png"),
        (IconTransformerUrl, "transformer.png"),
    ];

    private readonly DailyNoteOptions options;
    private Task? iconsPreparedTask;

    [GeneratedConstructor]
    public partial DailyNoteTileOperation(IServiceProvider serviceProvider);

    /// <summary>
    /// 磁贴通知的图片只能通过 ms-appx:///、ms-appdata:///local/ 或 http(s):// 引用，
    /// 没有 localcache 协议，因此缓存目录必须落在 LocalFolder 而不是 LocalCacheFolder。
    /// </summary>
    private static string BackgroundCacheDirectory
    {
        get => Path.Combine(HutaoRuntime.LocalFolderDirectory, "TileBackgrounds");
    }

    private static string IconCacheDirectory
    {
        get => Path.Combine(HutaoRuntime.LocalFolderDirectory, "TileIcons");
    }

    /// <summary>
    /// 为指定账户下载名片并裁剪为磁贴背景图（宽版 31:15 与大版 1:1）。
    /// </summary>
    public async ValueTask<bool> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri)
    {
        byte[] bytes = await DownloadImageAsync(nameCardUri).ConfigureAwait(false);

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

        options.TileBackgroundAvatarId.Value = avatarId;
        options.TileBackgroundUid.Value = uid;
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
    private static async Task<byte[]> DownloadImageAsync(Uri uri)
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
                try
                {
                    using (HttpResponseMessage response = await client.GetAsync(uri).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    lastError = ex;
                    if (attempt < DownloadMaxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt)).ConfigureAwait(false);
                    }
                }
            }

            throw new HttpRequestException($"下载图片失败：{uri}", lastError);
        }
    }

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

    /// <summary>
    /// 磁贴图片先写 PNG；超过体积上限时改写成 JPEG，查找时按此顺序回退。
    /// </summary>
    private static readonly string[] BackgroundImageExtensions = [".png", ".jpg"];

    /// <summary>
    /// 按目标宽高比居中裁剪图片并保存为 PNG，同时缩放到磁贴允许的尺寸与体积内。
    /// </summary>
    private static void CropAndSave(Image image, string path, double targetRatio)
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

    private static (int Width, int Height) ScaleToFit(int width, int height, int maxSize)
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
    private static void SaveCompressedPng(Bitmap output, string path)
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
    /// 清除磁贴背景设置。
    /// </summary>
    public void ClearBackground()
    {
        options.TileBackgroundAvatarId.Value = string.Empty;
        options.TileBackgroundUid.Value = string.Empty;
    }

    /// <summary>
    /// 准备图标缓存（幂等）。失败时清空缓存的任务，以便下次调用重新尝试。
    /// </summary>
    public Task PrepareIconsAsync()
    {
        if (iconsPreparedTask is { IsFaulted: false, IsCanceled: false })
        {
            return iconsPreparedTask;
        }

        iconsPreparedTask = PrepareIconsCoreAsync();
        return iconsPreparedTask;
    }

    private async Task PrepareIconsCoreAsync()
    {
        string directory = IconCacheDirectory;
        Directory.CreateDirectory(directory);

        await Task.WhenAll(IconEntries.Select(entry => DownloadAndTrimAsync(entry, directory))).ConfigureAwait(false);
    }

    private static async Task DownloadAndTrimAsync((string Url, string FileName) entry, string directory)
    {
        string outputPath = Path.Combine(directory, entry.FileName);
        if (File.Exists(outputPath))
        {
            return;
        }

        byte[] bytes = await DownloadImageAsync(new Uri(entry.Url)).ConfigureAwait(false);
        using (Bitmap bitmap = new(new MemoryStream(bytes)))
        {
            TrimAndCenter(bitmap, outputPath);
        }
    }

    private static void TrimAndCenter(Bitmap source, string outputPath)
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

        // 裁掉透明边后再压到磁贴实际需要的尺寸（磁贴按容器缩放，过大的源图毫无意义）。
        (int drawW, int drawH) = ScaleToFit(contentW, contentH, MaxTileIconPixelSize);
        int size = Math.Max(drawW, drawH);

        using (Bitmap output = new(size, size))
        {
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source,
                    new Rectangle((size - drawW) / 2, (size - drawH) / 2, drawW, drawH),
                    new Rectangle(minX, minY, contentW, contentH),
                    GraphicsUnit.Pixel);
            }

            output.Save(outputPath, ImageFormat.Png);
        }
    }

    private static string? ResolveIconSource(string url)
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

    internal bool TryUpdatePrimaryTile(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        if (RuntimeEnvironment.IsUnpackaged)
        {
            return false;
        }

        PrepareIconsAsync().SafeForget();

        // 磁贴更新只是锦上添花，任何一步失败都不应该让调用方的数据库写入回滚或抛出。
        try
        {
            string tileXml = entries.Count == 0
                ? BuildBackgroundOnlyTileXml()
                : BuildTileXml(entries);

            XmlDocument document = new();
            document.LoadXml(tileXml);

            TileNotification notification = new(document);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            return true;
        }
        catch (Exception ex)
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Update primary tile failed", "DailyNoteTileOperation", [("error", ex.GetType().Name)]));
            return false;
        }
    }

    private string? ResolveBackgroundImageSource(bool isWide)
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

    private string BuildBackgroundOnlyTileXml()
    {
        string? bg = ResolveBackgroundImageSource(true);
        string? bgSquare = ResolveBackgroundImageSource(false);
        if (string.IsNullOrEmpty(bg))
        {
            return "<tile/>";
        }

        return $"""
            <tile>
                <visual version="3" addImageQuery="false">
                    <binding template="TileWide">
                        <image src="{bg}" placement="background" addImageQuery="false"/>
                    </binding>
                    <binding template="TileLarge">
                        <image src="{bgSquare}" placement="background" addImageQuery="false"/>
                    </binding>
                </visual>
            </tile>
            """;
    }

    private string BuildTileXml(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        return entries.Count == 1
            ? BuildSingleEntryXml(entries.First())
            : BuildMultipleEntriesXml(entries);
    }

    private string BuildSingleEntryXml(DailyNoteEntry entry)
    {
        if (entry.DailyNote is not { } dailyNote)
        {
            return BuildBackgroundOnlyTileXml();
        }

        string? bg = ResolveBackgroundImageSource(true);
        string? bgSquare = ResolveBackgroundImageSource(false);

        return $"""
            <tile>
                <visual version="3" addImageQuery="false">
                    {BuildBindingXml("TileWide", bg, BuildSingleEntryWideRows(dailyNote))}
                    {BuildBindingXml("TileLarge", bgSquare, BuildSingleEntryLargeRows(dailyNote))}
                </visual>
            </tile>
            """;
    }

    /// <summary>
    /// 宽磁贴只有 310x150，纵向最多容下三行内容，四项明细竖排必然被裁掉最后一项。
    /// 这里改用两列布局：每行放「图标+文本」两组，两项占一行，两行即可容纳四项。
    /// </summary>
    /// <summary>
    /// 单账户宽磁贴。310x150 且带图标的行高约 50px，可用高度只够三行，
    /// 因此自上而下固定展示原粹树脂、每日委托、探索派遣；
    /// 洞天宝钱与参量质变仪交给大磁贴。
    /// </summary>
    private static string BuildSingleEntryWideRows(WebDailyNote dailyNote)
    {
        StringBuilder sb = new();
        sb.Append(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), WideRowHintStyle));
        sb.Append(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), WideRowHintStyle));

        if (BuildExpeditionValue(dailyNote) is { Length: > 0 } expeditionText)
        {
            sb.Append(BuildIconRow(IconExpeditionUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierExpedition, expeditionText), WideRowHintStyle));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 宽磁贴 310x150 需要在纵向放下标题加明细，因此统一使用最小的 caption 字号。
    /// 不能改用 captionSubtle：Subtle 系列是半透明样式，会把正文显示得发灰。
    /// </summary>
    private const string WideRowHintStyle = "caption";

    /// <summary>
    /// 大磁贴（310x310）空间宽裕，展示全部五项并按空行撑开间距。
    /// </summary>
    private static string BuildSingleEntryLargeRows(WebDailyNote dailyNote)
    {
        List<string> rows = [];
        rows.Add(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), "base"));
        rows.Add(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), "caption"));

        if (BuildExpeditionValue(dailyNote) is { Length: > 0 } expeditionText)
        {
            rows.Add(BuildIconRow(IconExpeditionUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierExpedition, expeditionText), "caption"));
        }

        rows.Add(BuildIconRow(IconHomeCoinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierHomeCoin, dailyNote.FormattedHomeCoin), "caption"));

        if (dailyNote.Transformer is { Obtained: true } transformer)
        {
            // 显示剩余冷却时间（如「2小时30分钟」），而不是「已准备完成」这类状态文案。
            string transformerText = transformer.RecoveryTime.TimeLeftFormatted;
            rows.Add(BuildIconRow(IconTransformerUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierTransformer, transformerText), "caption"));
        }

        return BuildLargeEntryRows(rows);
    }

    /// <summary>
    /// 大磁贴（310x310）空间宽裕，单账户只有 3~5 行，全部堆在顶部会显得局促，
    /// 在每行之间插入一行空白 caption 文本撑开间距。
    /// 注意：rows 中每一项都是完整的 group 块，分隔符本身不能再带 group 标签。
    /// </summary>
    private static string BuildLargeEntryRows(List<string> rows)
    {
        if (rows.Count <= 1)
        {
            return string.Join(string.Empty, rows);
        }

        return string.Join("<text hint-style=\"captionSubtle\"> </text>", rows);
    }

    private string BuildMultipleEntriesXml(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        // 宽磁贴只有 310x150，装不下多个账户，只展示首个账户；大磁贴才铺开全部账户。
        string? wideBlock = null;
        List<string> accountBlocks = [];
        foreach (DailyNoteEntry entry in entries)
        {
            WebDailyNote? dailyNote = entry.DailyNote;
            if (dailyNote is null)
            {
                continue;
            }

            wideBlock ??= BuildAccountWideBlock(entry, dailyNote);
            accountBlocks.Add(BuildAccountBlock(entry, dailyNote));
        }

        string? bg = ResolveBackgroundImageSource(true);
        string? bgSquare = ResolveBackgroundImageSource(false);

        return $"""
            <tile>
                <visual version="3" addImageQuery="false">
                    {BuildBindingXml("TileWide", bg, wideBlock ?? string.Empty)}
                    {BuildBindingXml("TileLarge", bgSquare, string.Join(string.Empty, accountBlocks))}
                </visual>
            </tile>
            """;
    }

    /// <summary>
    /// 多账户宽磁贴：只放首个账户的账户名与两项明细，保持「树脂 → 委托」的阅读顺序。
    /// <para>
    /// 带图标的行高约 50px，是磁贴布局按容器缩放图标的结果（与图标图片的像素尺寸无关，
    /// 否则 256px 的图标会占满整块磁贴）。加上账户名占用的高度后，
    /// 竖排三行明细必然裁掉末行，因此这里只放两项。
    /// </para>
    /// </summary>
    private static string BuildAccountWideBlock(DailyNoteEntry entry, WebDailyNote dailyNote)
    {
        StringBuilder sb = new();
        sb.Append(BuildAccountTitleRow(entry, compact: true));
        sb.Append(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), WideRowHintStyle));
        sb.Append(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), WideRowHintStyle));
        return sb.ToString();
    }

    /// <summary>
    /// 账户标题行。昵称用 base（粗体），区服用更小的 captionSubtle，放在同一行。
    /// 磁贴的 subgroup 只能按 hint-weight 百分比分配宽度、无法按内容自适应，因此：
    /// 总权重取 50，首个空子群只占 2%（约 6px）作为最小左内边距；
    /// 昵称需要约 30% 才能单行显示（更窄会被逐字换行成竖排）；
    /// 区服同样给 30%，并关闭换行，避免被裁成不完整文本；末尾空群吃掉剩余空间。
    /// <para>
    /// 区服刻意用 Subtle（半透明）样式，它是次要信息；
    /// 但便笺明细行不能用 Subtle，否则正文会发灰。
    /// <paramref name="compact"/> 为 true 时改为单行小字（宽磁贴纵向只能容下四行，
    /// 粗体标题会把末行挤出去）。
    /// </para>
    /// </summary>
    private static string BuildAccountTitleRow(DailyNoteEntry entry, bool compact)
    {
        string nickname = entry.UserGameRole?.Nickname ?? entry.Uid;
        string regionName = entry.UserGameRole?.RegionName ?? string.Empty;

        if (compact)
        {
            if (regionName.Length == 0)
            {
                return $"<text hint-style=\"{WideRowHintStyle}\" hint-wrap=\"false\">{SecurityElement.Escape(nickname)}</text>";
            }

            // 宽磁贴同样区分主次：昵称用不透明的 WideRowHintStyle，区服用半透明的 captionSubtle。
            // captionSubtle 与 caption 行高相同，因此拆成两个子群不会多占纵向空间。
            // 区服需要约 80px 才不会被省略号截断（权重 14/50 ≈ 28%），昵称给 16/50 保证单行。
            return $"""
                <group>
                    <subgroup hint-weight="1" hint-textStacking="center"><text hint-style="{WideRowHintStyle}"> </text></subgroup>
                    <subgroup hint-weight="16" hint-textStacking="center"><text hint-style="{WideRowHintStyle}" hint-wrap="false">{SecurityElement.Escape(nickname)}</text></subgroup>
                    <subgroup hint-weight="14" hint-textStacking="center"><text hint-style="captionSubtle" hint-wrap="false">{SecurityElement.Escape(regionName)}</text></subgroup>
                    <subgroup hint-weight="19" hint-textStacking="center"><text hint-style="{WideRowHintStyle}"> </text></subgroup>
                </group>
                """;
        }

        if (regionName.Length == 0)
        {
            return $"<text hint-style=\"base\" hint-wrap=\"true\">{SecurityElement.Escape(nickname)}</text>";
        }

        return $"""
            <group>
                <subgroup hint-weight="1" hint-textStacking="center"><text hint-style="base"> </text></subgroup>
                <subgroup hint-weight="15" hint-textStacking="center"><text hint-style="base" hint-wrap="true">{SecurityElement.Escape(nickname)}</text></subgroup>
                <subgroup hint-weight="15" hint-textStacking="center"><text hint-style="captionSubtle" hint-wrap="false">{SecurityElement.Escape(regionName)}</text></subgroup>
                <subgroup hint-weight="19" hint-textStacking="center"><text hint-style="base"> </text></subgroup>
            </group>
            """;
    }

    /// <summary>
    /// 单个账户在大磁贴上的展示块：标题行（昵称 + 区服）+ 树脂 + 每日委托 + 探索派遣。
    /// </summary>
    private static string BuildAccountBlock(DailyNoteEntry entry, WebDailyNote dailyNote)
    {
        StringBuilder sb = new();
        sb.Append(BuildAccountTitleRow(entry, compact: false));

        sb.Append(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), "caption"));
        sb.Append(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), "caption"));

        if (BuildExpeditionValue(dailyNote) is { Length: > 0 } expeditionText)
        {
            sb.Append(BuildIconRow(IconExpeditionUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierExpedition, expeditionText), "caption"));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 探索派遣的数值部分（如 <c>2/4</c>）；没有进行中的派遣时返回空串。
    /// 标签由调用方通过 <c>ServiceDailyNoteNotifierExpedition</c> 补齐，
    /// 这里不能再返回带「探索派遣」字样的整句，否则标签会重复。
    /// </summary>
    private static string BuildExpeditionValue(WebDailyNote dailyNote)
    {
        if (dailyNote.CurrentExpeditionNum <= 0)
        {
            return string.Empty;
        }

        int ongoing = 0;
        int total = dailyNote.Expeditions.Count;
        foreach (Expedition expedition in dailyNote.Expeditions)
        {
            if (expedition.Status == ExpeditionStatus.Ongoing)
            {
                ongoing++;
            }
        }

        return ongoing == 0
            ? SH.ServiceDailyNoteNotifierExpeditionAdaptiveHint
            : SH.FormatServiceDailyNoteNotifierExpeditionProgress(ongoing, total);
    }

    private static string BuildBindingXml(string template, string? background, string rows)
    {
        // 既没有背景图也没有内容时输出空 binding，磁贴会变成一片空白，直接省略
        if (string.IsNullOrEmpty(background) && string.IsNullOrEmpty(rows))
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        sb.Append($"        <binding template=\"{template}\">");

        if (!string.IsNullOrEmpty(background))
        {
            sb.Append($"<image src=\"{background}\" placement=\"background\" addImageQuery=\"false\"/>");
        }

        sb.Append(rows);
        sb.Append("</binding>");
        return sb.ToString();
    }

    private static string BuildIconRow(string icon, string text, string hintStyle)
    {
        string iconSource = ResolveIconSource(icon) ?? icon;
        return $"""
            <group>
                <subgroup hint-weight="1" hint-textStacking="center"><image src="{iconSource}" hint-crop="circle"/></subgroup>
                <subgroup hint-weight="9" hint-textStacking="center"><text hint-style="{hintStyle}" hint-wrap="true">{SecurityElement.Escape(text)}</text></subgroup>
            </group>
            """;
    }
}