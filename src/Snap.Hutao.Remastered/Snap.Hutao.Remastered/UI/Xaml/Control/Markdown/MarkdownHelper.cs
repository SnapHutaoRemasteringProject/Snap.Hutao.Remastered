// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal static partial class MarkdownHelper
{
    private const double ListItemIndent = 16;
    private const double NestedListItemIndent = 20;
    private const string TaskListIconFontFamilyName = "Segoe Fluent Icons";
    private const string TaskListUncheckedGlyph = "\uE739";
    private const string TaskListCheckedGlyph = "\uE73A";

    public static IReadOnlyList<UIElement> CreateMarkdownBlocks(string? markdown)
    {
        List<UIElement> blocks = [];
        RichTextBlock? richText = null;

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return blocks;
        }

        bool inCodeBlock = false;
        StringBuilder? codeBuffer = null;
        string codeLanguage = string.Empty;
        int codeFenceLength = 0;
        SpanLineEnumerator enumerator = markdown.AsSpan().EnumerateLines();
        int lineStart = 0;
        MarkdownLine? pendingLine = null;

        while (TryReadLine(markdown, ref enumerator, ref lineStart, ref pendingLine, out ReadOnlySpan<char> line, out MarkdownLine markdownLine))
        {
            ReadOnlySpan<char> trimmedStart = line.TrimStart();

            if (!inCodeBlock && MarkdownSyntax.TryParseBacktickCodeFence(trimmedStart, MarkdownSyntax.MinimumCodeFenceLength, out MarkdownCodeFenceSegment openingFence))
            {
                inCodeBlock = true;
                codeFenceLength = openingFence.FenceLength;
                codeLanguage = openingFence.FenceInfo.Trim().ToString();

                continue;
            }

            if (inCodeBlock)
            {
                if (MarkdownSyntax.TryParseBacktickCodeFence(trimmedStart, codeFenceLength, out MarkdownCodeFenceSegment closingFence) && closingFence.FenceInfo.IsWhiteSpace())
                {
                    AddCodeBlock(blocks, GetCodeBlockText(codeBuffer), codeLanguage);
                    codeBuffer?.Clear();
                    codeLanguage = string.Empty;
                    codeFenceLength = 0;
                    inCodeBlock = false;
                    richText = null;
                    continue;
                }

                codeBuffer ??= new StringBuilder();
                codeBuffer.Append(line);
                codeBuffer.Append('\n');
                continue;
            }

            if (line.IsWhiteSpace())
            {
                continue;
            }

            if (MarkdownSyntax.IsTableLine(line)
                && TryReadLine(markdown, ref enumerator, ref lineStart, ref pendingLine, out ReadOnlySpan<char> separatorLine, out MarkdownLine separatorMarkdownLine))
            {
                if (MarkdownSyntax.IsTableSeparator(separatorLine))
                {
                    AddTable(blocks, markdown, markdownLine, separatorMarkdownLine, ref enumerator, ref lineStart, ref pendingLine);
                    richText = null;
                    continue;
                }

                pendingLine = separatorMarkdownLine;
            }

            richText = EnsureRichTextBlock(blocks, richText);

            if (MarkdownSyntax.TryParseHeading(line, out ReadOnlySpan<char> headingText, out int headingLevel))
            {
                AddHeading(richText, headingText, headingLevel);
            }
            else if (MarkdownSyntax.IsHorizontalRule(line))
            {
                AddHorizontalRule(richText);
            }
            else if (MarkdownSyntax.TryParseBlockquote(line, out int blockquoteDepth, out ReadOnlySpan<char> blockquoteText))
            {
                AddBlockquote(richText, blockquoteDepth, blockquoteText);
            }
            else if (MarkdownSyntax.TryParseTaskListItem(line, out bool isTaskChecked, out ReadOnlySpan<char> taskText, out int taskListDepth))
            {
                AddTaskListItem(richText, isTaskChecked, taskText, taskListDepth);
            }
            else if (MarkdownSyntax.TryParseUnorderedListItem(line, out ReadOnlySpan<char> listItemText, out int unorderedListDepth))
            {
                AddListItem(richText, listItemText, "- ", unorderedListDepth);
            }
            else if (MarkdownSyntax.TryParseOrderedListItem(line, out ReadOnlySpan<char> listNumber, out ReadOnlySpan<char> listText, out int orderedListDepth))
            {
                AddListItem(richText, listText, CreateOrderedListBullet(listNumber), orderedListDepth);
            }
            else
            {
                AddParagraph(richText, line);
            }
        }

        if (inCodeBlock)
        {
            AddCodeBlock(blocks, GetCodeBlockText(codeBuffer), codeLanguage);
        }

        return blocks;
    }

    private static RichTextBlock EnsureRichTextBlock(List<UIElement> blocks, RichTextBlock? richText)
    {
        if (richText is not null)
        {
            return richText;
        }

        richText = new()
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14, // BodyTextBlockFontSize
            IsTextSelectionEnabled = true,
        };
        blocks.Add(richText);

        return richText;
    }

    private static bool TryReadLine(string markdown, ref SpanLineEnumerator enumerator, ref int lineStart, ref MarkdownLine? pendingLine, out ReadOnlySpan<char> line, out MarkdownLine markdownLine)
    {
        if (pendingLine.HasValue)
        {
            markdownLine = pendingLine.GetValueOrDefault();
            line = markdownLine.GetContent(markdown.AsSpan());
            pendingLine = null;
            return true;
        }

        if (!enumerator.MoveNext())
        {
            line = default;
            markdownLine = default;
            return false;
        }

        line = enumerator.Current;
        int contentEnd = lineStart + line.Length;
        int lineEnd = GetNextLineStart(markdown, contentEnd);
        markdownLine = new(lineStart, contentEnd, lineEnd);
        lineStart = lineEnd;
        return true;
    }

    private static int GetNextLineStart(string markdown, int lineEnd)
    {
        if ((uint)lineEnd >= (uint)markdown.Length)
        {
            return lineEnd;
        }

        if (markdown[lineEnd] is '\r' && lineEnd + 1 < markdown.Length && markdown[lineEnd + 1] is '\n')
        {
            return lineEnd + 2;
        }

        if (IsLineEnding(markdown[lineEnd]))
        {
            return lineEnd + 1;
        }

        return lineEnd;
    }

    private static bool IsLineEnding(char character)
    {
        return character is '\r' or '\n' or '\f' or '\u0085' or '\u2028' or '\u2029';
    }

    private static string GetCodeBlockText(StringBuilder? codeBuffer)
    {
        if (codeBuffer is null || codeBuffer.Length == 0)
        {
            return string.Empty;
        }

        int length = codeBuffer.Length;
        while (length > 0 && (codeBuffer[length - 1] == '\r' || codeBuffer[length - 1] == '\n'))
        {
            length--;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        return codeBuffer.ToString(0, length);
    }

    private static string CreateOrderedListBullet(ReadOnlySpan<char> number)
    {
        return string.Concat(number, ". ".AsSpan());
    }

    private static void AddHeading(RichTextBlock richText, ReadOnlySpan<char> text, int level)
    {
        Paragraph paragraph = new() { FontSize = GetHeadingFontSize(level), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 2) };
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static double GetHeadingFontSize(int level)
    {
        return level switch
        {
            1 => 28,
            2 => 21,
            3 => 17.5,
            4 => 14,
            5 => 12.25,
            6 => 11.9,
            _ => 14,
        };
    }

    private static void AddHorizontalRule(RichTextBlock richText)
    {
        Paragraph paragraph = new() { Margin = new Thickness(0) };
        paragraph.Inlines.Add(new InlineUIContainer
        {
            Child = new Border
            {
                Margin = new Thickness(0, 4, 0, 4),
                Height = 1,
                MinWidth = 100000,
                Background = GetThemeBrush("ControlStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
            },
        });
        richText.Blocks.Add(paragraph);
    }

    private static void AddBlockquote(RichTextBlock richText, int depth, ReadOnlySpan<char> text)
    {
        RichTextBlock quoteText = new()
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            IsTextSelectionEnabled = true,
        };

        Paragraph quoteParagraph = new()
        {
            Margin = new Thickness(0),
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush", "SystemControlForegroundBaseMediumBrush"),
        };
        AddInlineMarkdown(quoteParagraph.Inlines, text);
        quoteText.Blocks.Add(quoteParagraph);

        Brush borderBrush = GetThemeBrush("TextFillColorSecondaryBrush", "SystemControlForegroundBaseMediumBrush");
        UIElement quoteElement = quoteText;
        for (int i = 0; i < depth; i++)
        {
            quoteElement = new Border
            {
                Padding = new Thickness(8, i == 0 ? 2 : 0, 0, i == 0 ? 2 : 0),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(2, 0, 0, 0),
                Child = quoteElement,
            };
        }

        Paragraph container = new() { Margin = new Thickness(0, 2, 0, 2) };
        container.Inlines.Add(new InlineUIContainer
        {
            Child = quoteElement,
        });
        richText.Blocks.Add(container);
    }

    private static void AddTaskListItem(RichTextBlock richText, bool isChecked, ReadOnlySpan<char> text, int depth)
    {
        Paragraph paragraph = new() { Margin = CreateListItemMargin(depth) };
        paragraph.Inlines.Add(new Run
        {
            FontFamily = new FontFamily(TaskListIconFontFamilyName),
            FontSize = 14,
            Text = (isChecked ? TaskListCheckedGlyph : TaskListUncheckedGlyph) + " ",
        });
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddListItem(RichTextBlock richText, ReadOnlySpan<char> text, string bullet, int depth)
    {
        Paragraph paragraph = new() { Margin = CreateListItemMargin(depth) };
        paragraph.Inlines.Add(new Run { Text = bullet });
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static Thickness CreateListItemMargin(int depth)
    {
        return new Thickness(ListItemIndent + depth * NestedListItemIndent, 1, 0, 1);
    }

    private static void AddParagraph(RichTextBlock richText, ReadOnlySpan<char> text)
    {
        Paragraph paragraph = new() { Margin = new Thickness(0, 2, 0, 2) };
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddCodeBlock(List<UIElement> blocks, string code, string language)
    {
        blocks.Add(new MarkdownCodeBlockView
        {
            Code = code,
            CodeLanguage = language,
        });
    }

    private static List<string> ParseTableCells(ReadOnlySpan<char> line)
    {
        ReadOnlySpan<char> trimmed = MarkdownSyntax.TrimTableCellBounds(line);
        List<string> cells = [];
        int cellStart = 0;

        for (int i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] != '|' || MarkdownSyntax.IsEscaped(trimmed, i))
            {
                continue;
            }

            cells.Add(trimmed.Slice(cellStart, Math.Max(i - cellStart, 0)).Trim().ToString());
            cellStart = i + 1;
        }

        cells.Add(trimmed.Slice(cellStart, Math.Max(trimmed.Length - cellStart, 0)).Trim().ToString());
        return cells;
    }

    private static List<TextAlignment> ParseTableColumnAlignments(ReadOnlySpan<char> line, int columnCount)
    {
        List<TextAlignment> alignments = [];

        foreach (string cell in ParseTableCells(line))
        {
            alignments.Add(ParseTableColumnAlignment(cell));
        }

        while (alignments.Count < columnCount)
        {
            alignments.Add(TextAlignment.Left);
        }

        return alignments;
    }

    private static TextAlignment ParseTableColumnAlignment(string cell)
    {
        ReadOnlySpan<char> trimmed = cell.AsSpan().Trim();
        bool hasLeftMarker = trimmed.Length > 0 && trimmed[0] == ':';
        bool hasRightMarker = trimmed.Length > 0 && trimmed[trimmed.Length - 1] == ':';

        if (hasLeftMarker && hasRightMarker)
        {
            return TextAlignment.Center;
        }

        if (hasRightMarker)
        {
            return TextAlignment.Right;
        }

        return TextAlignment.Left;
    }

    private static void AddTable(
        List<UIElement> blocks,
        string markdown,
        MarkdownLine headerLine,
        MarkdownLine separatorLine,
        ref SpanLineEnumerator enumerator,
        ref int lineStart,
        ref MarkdownLine? pendingLine)
    {
        ReadOnlySpan<char> markdownSpan = markdown.AsSpan();
        List<string> headerCells = ParseTableCells(headerLine.GetContent(markdownSpan));
        int columnCount = headerCells.Count;
        List<TextAlignment> columnAlignments = ParseTableColumnAlignments(separatorLine.GetContent(markdownSpan), columnCount);
        List<IReadOnlyList<string>> rows = [headerCells];

        while (TryReadLine(markdown, ref enumerator, ref lineStart, ref pendingLine, out ReadOnlySpan<char> line, out MarkdownLine markdownLine))
        {
            if (!MarkdownSyntax.IsTableLine(line))
            {
                pendingLine = markdownLine;
                break;
            }

            List<string> cells = ParseTableCells(line);
            while (cells.Count < columnCount)
            {
                cells.Add(string.Empty);
            }

            rows.Add(cells);
        }

        blocks.Add(new MarkdownTableView
        {
            ColumnAlignments = columnAlignments,
            Rows = rows,
        });
    }

    private static void AddInlineMarkdown(InlineCollection inlines, ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return;
        }

        AddInlineMarkdown(inlines, text.ToString());
    }

    internal static void AddInlineMarkdown(InlineCollection inlines, string text)
    {
        AddInlineMarkdown(inlines, text, null);
    }

    private static void AddInlineMarkdown(InlineCollection inlines, string text, global::Windows.UI.Text.TextDecorations? textDecorations)
    {
        if (text.Length == 0)
        {
            return;
        }

        Regex pattern = InlineMarkdownPattern();
        int lastIndex = 0;
        int searchIndex = 0;

        while (searchIndex < text.Length)
        {
            Match match = pattern.Match(text, searchIndex);
            if (!match.Success)
            {
                break;
            }

            if (IsInlineMarkdownMatchEscaped(text, match))
            {
                searchIndex = match.Index + 1;
                continue;
            }

            if (match.Index > lastIndex)
            {
                AddTextRun(inlines, text[lastIndex..match.Index], textDecorations);
            }

            if (match.Groups["bolditalic"].Success)
            {
                Bold bold = new();
                Italic italic = new();
                AddInlineMarkdown(italic.Inlines, match.Groups["bolditalic"].Value, textDecorations);
                bold.Inlines.Add(italic);
                inlines.Add(bold);
            }
            else if (match.Groups["bold"].Success)
            {
                Bold bold = new();
                AddInlineMarkdown(bold.Inlines, match.Groups["bold"].Value, textDecorations);
                inlines.Add(bold);
            }
            else if (match.Groups["italic"].Success)
            {
                Italic italic = new();
                AddInlineMarkdown(italic.Inlines, match.Groups["italic"].Value, textDecorations);
                inlines.Add(italic);
            }
            else if (match.Groups["strikethrough"].Success)
            {
                AddInlineMarkdown(
                    inlines,
                    match.Groups["strikethrough"].Value,
                    CombineTextDecorations(textDecorations, global::Windows.UI.Text.TextDecorations.Strikethrough));
            }
            else if (match.Groups["highlight"].Success)
            {
                AddHighlight(inlines, MarkdownSyntax.UnescapeMarkdownText(match.Groups["highlight"].Value));
            }
            else if (match.Groups["code"].Success)
            {
                inlines.Add(new InlineUIContainer
                {
                    Child = new MarkdownInlineCodeView
                    {
                        Text = MarkdownSyntax.NormalizeInlineCodeText(match.Groups["code"].Value),
                    },
                });
            }
            else if (match.Groups["linktext"].Success)
            {
                AddLink(
                    inlines,
                    MarkdownSyntax.UnescapeMarkdownText(match.Groups["linktext"].Value),
                    match.Groups["linkurl"].Value,
                    match.Groups["linktitle"].Value);
            }
            else if (match.Groups["imgalt"].Success)
            {
                AddImage(
                    inlines,
                    MarkdownSyntax.UnescapeMarkdownText(match.Groups["imgalt"].Value),
                    match.Groups["imgurl"].Value,
                    match.Groups["imgtitle"].Value);
            }

            lastIndex = match.Index + match.Length;
            searchIndex = lastIndex;
        }

        if (lastIndex == 0)
        {
            AddTextRun(inlines, text, textDecorations);
        }
        else if (lastIndex < text.Length)
        {
            AddTextRun(inlines, text[lastIndex..], textDecorations);
        }
    }

    private static global::Windows.UI.Text.TextDecorations CombineTextDecorations(
        global::Windows.UI.Text.TextDecorations? current,
        global::Windows.UI.Text.TextDecorations additional)
    {
        if (current.HasValue)
        {
            return current.Value | additional;
        }

        return additional;
    }

    private static void AddTextRun(InlineCollection inlines, string text, global::Windows.UI.Text.TextDecorations? textDecorations)
    {
        string unescapedText = MarkdownSyntax.UnescapeMarkdownText(text);
        if (unescapedText.Length == 0)
        {
            return;
        }

        Run run = new()
        {
            Text = unescapedText,
        };

        if (textDecorations.HasValue)
        {
            run.TextDecorations = textDecorations.Value;
        }

        inlines.Add(run);
    }

    private static bool IsInlineMarkdownMatchEscaped(string text, Match match)
    {
        if (MarkdownSyntax.IsEscaped(text, match.Index))
        {
            return true;
        }

        if (match.Groups["bolditalic"].Success)
        {
            return MarkdownSyntax.IsEscaped(text, match.Index + match.Length - 3);
        }

        if (match.Groups["bold"].Success
            || match.Groups["strikethrough"].Success
            || match.Groups["highlight"].Success)
        {
            return MarkdownSyntax.IsEscaped(text, match.Index + match.Length - 2);
        }

        if (match.Groups["italic"].Success)
        {
            return MarkdownSyntax.IsEscaped(text, match.Index + match.Length - 1);
        }

        return false;
    }

    private static void AddHighlight(InlineCollection inlines, string text)
    {
        inlines.Add(new InlineUIContainer
        {
            Child = new Border
            {
                Padding = new Thickness(2, 0, 2, 0),
                CornerRadius = new CornerRadius(2),
                Background = GetThemeBrush("SystemFillColorCautionBackgroundBrush", "SystemControlHighlightAccentBrush"),
                Child = new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Foreground = GetThemeBrush("TextFillColorPrimaryBrush", "SystemControlForegroundBaseHighBrush"),
                    Text = text,
                    TextWrapping = TextWrapping.NoWrap,
                },
            },
        });
    }

    private static void AddLink(InlineCollection inlines, string text, string url, string title)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            inlines.Add(new Run { Text = CreateLinkFallback(text, url, title) });
            return;
        }

        Hyperlink link = new()
        {
            NavigateUri = uri,
        };
        link.Inlines.Add(new Run { Text = text });

        if (!string.IsNullOrEmpty(title))
        {
            ToolTipService.SetToolTip(link, title);
        }

        inlines.Add(link);
    }

    private static void AddImage(InlineCollection inlines, string altText, string url, string title)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            AddImageFallback(inlines, altText, url, title);
            return;
        }

        // Model output must not initiate requests to arbitrary image hosts.
        AddLink(inlines, string.IsNullOrWhiteSpace(altText) ? url : altText, url, title);
    }

    private static void AddImageFallback(InlineCollection inlines, string altText, string url, string title)
    {
        inlines.Add(new Run
        {
            Text = CreateImageFallback(altText, url, title),
            FontStyle = global::Windows.UI.Text.FontStyle.Italic,
        });
    }

    private static string CreateLinkFallback(string text, string url, string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return $"[{text}]({url})";
        }

        return $"[{text}]({url} \"{title}\")";
    }

    private static string CreateImageFallback(string altText, string url, string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return $"![{altText}]({url})";
        }

        return $"![{altText}]({url} \"{title}\")";
    }

    internal static Brush GetThemeBrush(string resourceKey, string fallbackResourceKey)
    {
        try
        {
            return (Brush)Application.Current.Resources[resourceKey];
        }
        catch
        {
            try
            {
                return (Brush)Application.Current.Resources[fallbackResourceKey];
            }
            catch
            {
                return new SolidColorBrush(Colors.Gold);
            }
        }
    }

    [GeneratedRegex(@"\*\*\*(?<bolditalic>(?:\\.|.)+?)\*\*\*|\*\*(?<bold>(?:\\.|.)+?)\*\*|(?<!\*)\*(?!\*)(?<italic>(?:\\.|.)+?)(?<!\*)\*(?!\*)|~~(?<strikethrough>(?:\\.|.)+?)~~|==(?<highlight>(?:\\.|.)+?)==|(?<codeDelimiter>`+)(?<code>.*?)(?<!`)\k<codeDelimiter>(?!`)|!\[(?<imgalt>(?:\\.|[^\]])*?)\]\((?<imgurl>\S+?)(?:\s+""(?<imgtitle>[^""]*?)"")?\)|\[(?<linktext>(?:\\.|[^\]])+?)\]\((?<linkurl>\S+?)(?:\s+""(?<linktitle>[^""]*?)"")?\)")]
    private static partial Regex InlineMarkdownPattern();
}
