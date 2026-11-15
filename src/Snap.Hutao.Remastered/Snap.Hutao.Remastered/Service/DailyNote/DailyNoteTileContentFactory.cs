// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;
using System.Security;
using System.Text;
using static Snap.Hutao.Remastered.Service.DailyNote.DailyNoteTileImageCache;
using WebDailyNote = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote.DailyNote;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 每日便签磁贴内容工厂：把便笺条目的数据渲染成开始菜单磁贴所需的 XML。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IDailyNoteTileContentFactory))]
internal sealed partial class DailyNoteTileContentFactory : IDailyNoteTileContentFactory
{
    /// <summary>
    /// 宽磁贴 310x150 需要在纵向放下标题加明细，因此统一使用最小的 caption 字号。
    /// 不能改用 captionSubtle：Subtle 系列是半透明样式，会把正文显示得发灰。
    /// </summary>
    private const string WideRowHintStyle = "caption";

    private readonly IDailyNoteTileImageCache imageCache;

    [GeneratedConstructor]
    public partial DailyNoteTileContentFactory(IServiceProvider serviceProvider);

    public string? BuildTileXml(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        return entries.Count == 1
            ? BuildSingleEntryXml(entries.First())
            : BuildMultipleEntriesXml(entries);
    }

    /// <summary>
    /// 只带背景图的磁贴内容；没有背景图时返回 <see langword="null"/>，
    /// 表示没有任何内容可发布（调用方会改用 <c>TileUpdater.Clear()</c>）。
    /// </summary>
    public string? BuildBackgroundOnlyTileXml()
    {
        string? bg = imageCache.ResolveBackgroundImageSource(true);
        string? bgSquare = imageCache.ResolveBackgroundImageSource(false);
        if (string.IsNullOrEmpty(bg))
        {
            return null;
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

    private string? BuildSingleEntryXml(DailyNoteEntry entry)
    {
        if (entry.DailyNote is not { } dailyNote)
        {
            return BuildBackgroundOnlyTileXml();
        }

        string? bg = imageCache.ResolveBackgroundImageSource(true);
        string? bgSquare = imageCache.ResolveBackgroundImageSource(false);

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
    private string BuildSingleEntryWideRows(WebDailyNote dailyNote)
    {
        StringBuilder sb = new();
        // 树脂是首要资源，用比 caption 更大的 body 字号强调。
        sb.Append(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), "body"));
        sb.Append(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), TaskHintStyle(dailyNote, WideRowHintStyle)));

        if (BuildExpeditionValue(dailyNote) is { Length: > 0 } expeditionText)
        {
            sb.Append(BuildIconRow(IconExpeditionUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierExpedition, expeditionText), WideRowHintStyle));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 每日委托的文字样式：全部完成（4/4）时改用半透明的 captionSubtle，
    /// 用「已完成」的褪色效果提示用户；否则保持传入的普通样式。
    /// <para>
    /// 判断要求 TotalTaskNum 大于 0，避免便笺尚未加载（两者都是 0）时误判为已完成。
    /// </para>
    /// </summary>
    private string TaskHintStyle(WebDailyNote dailyNote, string normalStyle)
    {
        return dailyNote.TotalTaskNum > 0 && dailyNote.FinishedTaskNum >= dailyNote.TotalTaskNum
            ? "captionSubtle"
            : normalStyle;
    }

    /// <summary>
    /// 大磁贴（310x310）空间宽裕，展示全部五项并按空行撑开间距。
    /// </summary>
    private string BuildSingleEntryLargeRows(WebDailyNote dailyNote)
    {
        List<string> rows = [];
        rows.Add(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), "base"));
        rows.Add(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), TaskHintStyle(dailyNote, "caption")));

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
    private string BuildLargeEntryRows(List<string> rows)
    {
        if (rows.Count <= 1)
        {
            return string.Join(string.Empty, rows);
        }

        return string.Join("<text hint-style=\"captionSubtle\"> </text>", rows);
    }

    private string? BuildMultipleEntriesXml(IReadOnlyCollection<DailyNoteEntry> entries)
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

        // 所有账户都还没有便笺数据：退化为纯背景磁贴（没有背景时返回 null 交给调用方清空）。
        if (accountBlocks.Count == 0)
        {
            return BuildBackgroundOnlyTileXml();
        }

        string? bg = imageCache.ResolveBackgroundImageSource(true);
        string? bgSquare = imageCache.ResolveBackgroundImageSource(false);

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
    private string BuildAccountWideBlock(DailyNoteEntry entry, WebDailyNote dailyNote)
    {
        StringBuilder sb = new();
        sb.Append(BuildAccountTitleRow(entry, compact: true));
        sb.Append(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), WideRowHintStyle));
        sb.Append(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), TaskHintStyle(dailyNote, WideRowHintStyle)));
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
    private string BuildAccountTitleRow(DailyNoteEntry entry, bool compact)
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
    private string BuildAccountBlock(DailyNoteEntry entry, WebDailyNote dailyNote)
    {
        StringBuilder sb = new();
        sb.Append(BuildAccountTitleRow(entry, compact: false));

        sb.Append(BuildIconRow(IconResinUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierResin, dailyNote.FormattedResin), "caption"));
        sb.Append(BuildIconRow(IconTaskUrl, SH.FormatServiceDailyNoteTileLabelValue(SH.ServiceDailyNoteNotifierDailyTask, dailyNote.FormattedTask), TaskHintStyle(dailyNote, "caption")));

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
    private string BuildExpeditionValue(WebDailyNote dailyNote)
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

    private string BuildBindingXml(string template, string? background, string rows)
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

    private string BuildIconRow(string icon, string text, string hintStyle)
    {
        string iconSource = imageCache.ResolveIconSource(icon) ?? icon;
        return $"""
            <group>
                <subgroup hint-weight="1" hint-textStacking="center"><image src="{iconSource}" hint-crop="circle"/></subgroup>
                <subgroup hint-weight="9" hint-textStacking="center"><text hint-style="{hintStyle}" hint-wrap="true">{SecurityElement.Escape(text)}</text></subgroup>
            </group>
            """;
    }
}
