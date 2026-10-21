// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.Buffers;
using System.Text;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal static class MarkdownSyntax
{
    private static readonly SearchValues<char> LineEndings = SearchValues.Create("\r\f\u0085\u2028\u2029\n");
    private const int ListIndentColumnsPerLevel = 2;
    private const int TabIndentColumns = 4;

    internal const int MinimumCodeFenceLength = 3;

    internal static bool TryReadCompleteLine(ReadOnlySpan<char> markdown, ref int lineStart, ref MarkdownLine? pendingLine, out MarkdownLine line)
    {
        if (pendingLine.HasValue)
        {
            line = pendingLine.GetValueOrDefault();
            pendingLine = null;
            return true;
        }

        return TryReadCompleteLine(markdown, ref lineStart, out line);
    }

    internal static bool TryReadCompleteLine(ReadOnlySpan<char> markdown, ref int lineStart, out MarkdownLine line)
    {
        int offset = markdown[lineStart..].IndexOfAny(LineEndings);
        if (offset < 0)
        {
            line = default;
            return false;
        }

        int contentEnd = lineStart + offset;
        int lineEnd = contentEnd + 1;

        if (markdown[contentEnd] is '\r' && lineEnd < markdown.Length && markdown[lineEnd] is '\n')
        {
            lineEnd++;
        }

        line = new(lineStart, contentEnd, lineEnd);
        lineStart = lineEnd;
        return true;
    }

    internal static bool TryParseHeading(ReadOnlySpan<char> line, out ReadOnlySpan<char> text, out int level)
    {
        ReadOnlySpan<char> trimmed = line.TrimStart();
        level = GetHeadingLevel(trimmed);

        if (level is 0)
        {
            text = default;
            return false;
        }

        int textOffset = level;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        text = trimmed[Math.Min(textOffset, trimmed.Length)..];
        return true;
    }

    internal static bool IsHeadingLine(ReadOnlySpan<char> content)
    {
        return GetHeadingLevel(content.TrimStart()) is not 0;
    }

    internal static bool TryParseBacktickCodeFence(ReadOnlySpan<char> trimmedStart, int minimumFenceLength, out MarkdownCodeFenceSegment codeFence)
    {
        int fenceLength = CountLeadingBackticks(trimmedStart);

        if (fenceLength < minimumFenceLength)
        {
            codeFence = default;
            return false;
        }

        codeFence = new(fenceLength, trimmedStart[Math.Min(fenceLength, trimmedStart.Length)..]);
        return true;
    }

    internal static bool TryParseBacktickCodeFence(ReadOnlySpan<char> lineContent, int lineStart, int minimumFenceLength, out MarkdownCodeFenceLine codeFenceLine)
    {
        ReadOnlySpan<char> trimmedStart = lineContent.TrimStart();
        int startOffset = lineContent.Length - trimmedStart.Length;
        int fenceLength = CountLeadingBackticks(trimmedStart);

        if (fenceLength < minimumFenceLength)
        {
            codeFenceLine = default;
            return false;
        }

        codeFenceLine = new(fenceLength, lineStart + startOffset + fenceLength);
        return true;
    }

    internal static bool IsHorizontalRule(ReadOnlySpan<char> line)
    {
        ReadOnlySpan<char> span = line.Trim();

        if (span.Length < 3)
        {
            return false;
        }

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] is not ('-' or '*' or '_'))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool TryParseBlockquote(ReadOnlySpan<char> line, out int depth, out ReadOnlySpan<char> text)
    {
        ReadOnlySpan<char> trimmed = line.TrimStart();

        if (trimmed.Length is 0 || trimmed[0] is not '>')
        {
            depth = 0;
            text = default;
            return false;
        }

        int textOffset = 0;
        depth = 0;
        while (textOffset < trimmed.Length && trimmed[textOffset] is '>')
        {
            textOffset++;
            depth++;

            while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
            {
                textOffset++;
            }
        }

        text = trimmed[Math.Min(textOffset, trimmed.Length)..];
        return true;
    }

    internal static bool TryParseTaskListItem(ReadOnlySpan<char> line, out bool isChecked, out ReadOnlySpan<char> text, out int depth)
    {
        if (!TryParseUnorderedListItem(line, out ReadOnlySpan<char> listText, out depth))
        {
            isChecked = false;
            text = default;
            depth = 0;
            return false;
        }

        ReadOnlySpan<char> trimmed = listText.TrimStart();
        if (trimmed.Length < 3 || trimmed[0] is not '[' || trimmed[2] is not ']')
        {
            isChecked = false;
            text = default;
            depth = 0;
            return false;
        }

        char marker = trimmed[1];
        if (marker is not (' ' or 'x' or 'X'))
        {
            isChecked = false;
            text = default;
            depth = 0;
            return false;
        }

        int textOffset = 3;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        isChecked = marker is 'x' or 'X';
        text = trimmed[Math.Min(textOffset, trimmed.Length)..];
        return true;
    }

    internal static bool TryParseUnorderedListItem(ReadOnlySpan<char> line, out ReadOnlySpan<char> text, out int depth)
    {
        ReadOnlySpan<char> trimmed = line.TrimStart();

        if (trimmed.Length < 2 || trimmed[0] is not ('-' or '*' or '+') || !char.IsWhiteSpace(trimmed[1]))
        {
            text = default;
            depth = 0;
            return false;
        }

        int textOffset = 2;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        text = trimmed[Math.Min(textOffset, trimmed.Length)..];
        depth = GetListDepth(line);
        return true;
    }

    internal static bool TryParseOrderedListItem(ReadOnlySpan<char> line, out ReadOnlySpan<char> number, out ReadOnlySpan<char> text, out int depth)
    {
        ReadOnlySpan<char> trimmed = line.TrimStart();
        ReadOnlySpan<char> span = trimmed;
        int index = 0;

        while (index < span.Length && char.IsDigit(span[index]))
        {
            index++;
        }

        if (index is 0 || index + 1 >= span.Length || span[index] is not '.' || !char.IsWhiteSpace(span[index + 1]))
        {
            number = default;
            text = default;
            depth = 0;
            return false;
        }

        int textOffset = index + 2;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        number = trimmed[..Math.Max(index, 0)];
        text = trimmed[Math.Min(textOffset, trimmed.Length)..];
        depth = GetListDepth(line);
        return true;
    }

    internal static bool IsTableLine(ReadOnlySpan<char> lineContent)
    {
        ReadOnlySpan<char> trimmed = lineContent.Trim();

        return trimmed.Length > 2
            && trimmed[0] == '|'
            && trimmed[^1] == '|'
            && !IsEscaped(trimmed, trimmed.Length - 1);
    }

    internal static bool IsTableSeparator(ReadOnlySpan<char> lineContent)
    {
        ReadOnlySpan<char> span = lineContent.Trim();

        if (span.Length <= 2 || span[0] is not '|' || span[^1] is not '|')
        {
            return false;
        }

        for (int i = 1; i < span.Length - 1; i++)
        {
            if (span[i] is not ('|' or '-' or ':') && !char.IsWhiteSpace(span[i]))
            {
                return false;
            }
        }

        return true;
    }

    internal static ReadOnlySpan<char> TrimTableCellBounds(ReadOnlySpan<char> line)
    {
        ReadOnlySpan<char> trimmed = line.Trim();
        int start = 0;
        int length = trimmed.Length;

        while (length > 0 && trimmed[start] is '|')
        {
            start++;
            length--;
        }

        while (length > 0 && trimmed[start + length - 1] is '|')
        {
            length--;
        }

        return trimmed.Slice(start, Math.Max(length, 0));
    }

    internal static bool IsEscaped(string text, int index)
    {
        return IsEscaped(text.AsSpan(), index);
    }

    internal static bool IsEscaped(ReadOnlySpan<char> text, int index)
    {
        int backslashCount = 0;
        for (int i = index - 1; i >= 0 && text[i] is '\\'; i--)
        {
            backslashCount++;
        }

        return backslashCount % 2 != 0;
    }

    internal static string NormalizeInlineCodeText(string code)
    {
        string normalized = code.ReplaceLineEndings(" ");
        if (normalized.Length < 2 || normalized[0] is not ' ' || normalized[^1] is not ' ')
        {
            return normalized;
        }

        for (int i = 1; i < normalized.Length - 1; i++)
        {
            if (normalized[i] != ' ')
            {
                return normalized[1..^1];
            }
        }

        return normalized;
    }

    internal static string UnescapeMarkdownText(string text)
    {
        if (!text.Contains('\\'))
        {
            return text;
        }

        StringBuilder? builder = null;
        int textStart = 0;

        for (int i = 0; i < text.Length - 1; i++)
        {
            if (text[i] != '\\' || !IsMarkdownEscapableCharacter(text[i + 1]))
            {
                continue;
            }

            builder ??= new StringBuilder(text.Length);
            builder.Append(text.AsSpan(textStart, i - textStart));
            builder.Append(text[i + 1]);
            i++;
            textStart = i + 1;
        }

        if (builder is null)
        {
            return text;
        }

        builder.Append(text.AsSpan(textStart));
        return builder.ToString();
    }

    private static int GetHeadingLevel(ReadOnlySpan<char> content)
    {
        int level = content.IndexOfAnyExcept('#');

        if (level is (< 1 or > 6) || !char.IsWhiteSpace(content[level]))
        {
            return 0;
        }

        return level;
    }

    private static int CountLeadingBackticks(ReadOnlySpan<char> text)
    {
        int length = text.IndexOfAnyExcept('`');
        return length < 0 ? text.Length : length;
    }

    private static int GetListDepth(ReadOnlySpan<char> line)
    {
        int columns = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char current = line[i];
            if (current is ' ')
            {
                columns++;
            }
            else if (current is '\t')
            {
                columns += TabIndentColumns - columns % TabIndentColumns;
            }
            else if (char.IsWhiteSpace(current))
            {
                columns++;
            }
            else
            {
                break;
            }
        }

        return columns / ListIndentColumnsPerLevel;
    }

    private static bool IsMarkdownEscapableCharacter(char character)
    {
        return character is (>= '!' and <= '~') && (char.IsPunctuation(character) || char.IsSymbol(character));
    }
}
