// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Drawing;
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

    private readonly IHttpClientFactory httpClientFactory;
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
    /// 为指定账户下载名刺并裁剪为磁贴背景图（宽版 31:15 与大版 1:1）。
    /// </summary>
    public async ValueTask<bool> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri)
    {
        string directory = BackgroundCacheDirectory;
        Directory.CreateDirectory(directory);

        using (HttpClient httpClient = httpClientFactory.CreateClient(nameof(DailyNoteTileOperation)))
        {
            byte[] bytes = await httpClient.GetByteArrayAsync(nameCardUri).ConfigureAwait(false);
            using (Image image = Image.FromStream(new MemoryStream(bytes)))
            {
                CropAndSave(image, Path.Combine(directory, $"{avatarId}_wide.png"), 31d / 15d);
                CropAndSave(image, Path.Combine(directory, $"{avatarId}_large.png"), 1d);
            }
        }

        options.TileBackgroundAvatarId.Value = avatarId;
        options.TileBackgroundUid.Value = uid;
        return true;
    }

    /// <summary>
    /// 按目标宽高比居中裁剪图片并保存为 PNG。
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

        using (Bitmap output = new(width, height))
        {
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle(offsetX, offsetY, width, height), GraphicsUnit.Pixel);
            }

            output.Save(path, ImageFormat.Png);
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

        using (HttpClient httpClient = httpClientFactory.CreateClient(nameof(DailyNoteTileOperation)))
        {
            await Task.WhenAll(IconEntries.Select(entry => DownloadAndTrimAsync(httpClient, entry, directory))).ConfigureAwait(false);
        }
    }

    private static async Task DownloadAndTrimAsync(HttpClient httpClient, (string Url, string FileName) entry, string directory)
    {
        string outputPath = Path.Combine(directory, entry.FileName);
        if (File.Exists(outputPath))
        {
            return;
        }

        byte[] bytes = await httpClient.GetByteArrayAsync(entry.Url).ConfigureAwait(false);
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
        int size = Math.Max(contentW, contentH);

        using (Bitmap output = new(size, size))
        {
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(source,
                    new Rectangle((size - contentW) / 2, (size - contentH) / 2, contentW, contentH),
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
        string path = Path.Combine(BackgroundCacheDirectory, $"{avatarId}{suffix}.png");
        return File.Exists(path)
            ? $"ms-appdata:///local/TileBackgrounds/{avatarId}{suffix}.png"
            : null;
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

        // 探索派遣行
        string expeditionText = string.Empty;
        if (dailyNote.CurrentExpeditionNum > 0)
        {
            int ongoing = 0;
            int total = dailyNote.Expeditions.Count;
            foreach (Expedition expedition in dailyNote.Expeditions)
            {
                if (expedition.Status == ExpeditionStatus.Ongoing)
                {
                    ongoing++;
                }
            }

            expeditionText = ongoing == 0
                ? SH.ServiceDailyNoteNotifierExpeditionHint
                : SH.FormatServiceDailyNoteNotifierExpeditionCount(ongoing, total);
        }

        // 参量质变仪行
        string? transformerText = null;
        if (dailyNote.Transformer is { Obtained: true } transformer)
        {
            transformerText = transformer.RecoveryTime is null
                ? SH.WebDailyNoteTransformerReached
                : transformer.ObtainedAndReachedFormatted;
        }

        List<string> rows = [];
        rows.Add(BuildIconRow(IconResinUrl, dailyNote.FormattedResin, "base"));
        rows.Add(BuildIconRow(IconTaskUrl, dailyNote.FormattedTask, "caption"));
        rows.Add(BuildIconRow(IconHomeCoinUrl, dailyNote.FormattedHomeCoin, "caption"));

        if (!string.IsNullOrEmpty(expeditionText))
        {
            rows.Add(BuildIconRow(IconExpeditionUrl, expeditionText, "caption"));
        }

        if (transformerText is not null)
        {
            rows.Add(BuildIconRow(IconTransformerUrl, transformerText, "caption"));
        }

        string allRows = string.Join(string.Empty, rows);
        string? bg = ResolveBackgroundImageSource(true);
        string? bgSquare = ResolveBackgroundImageSource(false);

        return $"""
            <tile>
                <visual version="3" addImageQuery="false">
                    {BuildBindingXml("TileWide", bg, allRows)}
                    {BuildBindingXml("TileLarge", bgSquare, allRows)}
                </visual>
            </tile>
            """;
    }

    private string BuildMultipleEntriesXml(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        List<string> rows = [];
        foreach (DailyNoteEntry entry in entries)
        {
            WebDailyNote? dailyNote = entry.DailyNote;
            if (dailyNote is null)
            {
                continue;
            }

            string label = entry.UserGameRole?.ToString() ?? entry.Uid;
            rows.Add(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileAccountResin(label, dailyNote.FormattedResin), "caption"));
        }

        string allRows = string.Join(string.Empty, rows);
        string? bg = ResolveBackgroundImageSource(true);
        string? bgSquare = ResolveBackgroundImageSource(false);

        return $"""
            <tile>
                <visual version="3" addImageQuery="false">
                    {BuildBindingXml("TileWide", bg, allRows)}
                    {BuildBindingXml("TileLarge", bgSquare, allRows)}
                </visual>
            </tile>
            """;
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