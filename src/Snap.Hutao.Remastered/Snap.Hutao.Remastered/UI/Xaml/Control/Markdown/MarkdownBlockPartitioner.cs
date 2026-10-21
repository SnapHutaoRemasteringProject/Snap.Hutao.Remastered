// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal static class MarkdownBlockPartitioner
{
    public static int GetStablePrefixLength(ReadOnlySpan<char> markdown)
    {
        return GetStablePrefixLength(markdown, 0);
    }

    public static int GetStablePrefixLength(ReadOnlySpan<char> markdown, int startIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, markdown.Length);

        if (markdown.Length is 0)
        {
            return 0;
        }

        int stableEnd = startIndex;
        bool inCodeBlock = false;
        int codeFenceLength = 0;
        int nextLineStart = startIndex;
        MarkdownLine? pendingLine = null;

        while (MarkdownSyntax.TryReadCompleteLine(markdown, ref nextLineStart, ref pendingLine, out MarkdownLine line))
        {
            ReadOnlySpan<char> content = line.GetContent(markdown);

            if (!inCodeBlock && MarkdownSyntax.TryParseBacktickCodeFence(content, line.Start, MarkdownSyntax.MinimumCodeFenceLength, out MarkdownCodeFenceLine openingFenceLine))
            {
                inCodeBlock = true;
                codeFenceLength = openingFenceLine.FenceLength;

                continue;
            }

            if (inCodeBlock)
            {
                if (MarkdownSyntax.TryParseBacktickCodeFence(content, line.Start, codeFenceLength, out MarkdownCodeFenceLine closingFenceLine)
                    && markdown[closingFenceLine.FenceInfoStart..line.ContentEnd].IsWhiteSpace())
                {
                    inCodeBlock = false;
                    codeFenceLength = 0;
                    stableEnd = line.End;
                    continue;
                }

                continue;
            }

            if (MarkdownSyntax.IsHeadingLine(content) && line.Start > startIndex && !markdown[startIndex..line.Start].IsWhiteSpace())
            {
                return line.Start;
            }

            if (MarkdownSyntax.IsTableLine(content))
            {
                if (!MarkdownSyntax.TryReadCompleteLine(markdown, ref nextLineStart, ref pendingLine, out MarkdownLine nextLine))
                {
                    break;
                }

                if (MarkdownSyntax.IsTableSeparator(nextLine.GetContent(markdown)))
                {
                    MarkdownLine tableEndLine = nextLine;
                    while (MarkdownSyntax.TryReadCompleteLine(markdown, ref nextLineStart, ref pendingLine, out MarkdownLine tableLine))
                    {
                        if (!MarkdownSyntax.IsTableLine(tableLine.GetContent(markdown)))
                        {
                            pendingLine = tableLine;
                            break;
                        }

                        tableEndLine = tableLine;
                    }

                    if (!pendingLine.HasValue)
                    {
                        break;
                    }

                    stableEnd = tableEndLine.End;
                    continue;
                }

                pendingLine = nextLine;
            }

            stableEnd = line.End;
        }

        return stableEnd;
    }

    public static bool StartsWithHeading(ReadOnlySpan<char> markdown)
    {
        if (markdown.Length is 0)
        {
            return false;
        }

        int nextLineStart = 0;
        while (MarkdownSyntax.TryReadCompleteLine(markdown, ref nextLineStart, out MarkdownLine line))
        {
            ReadOnlySpan<char> content = line.GetContent(markdown);

            if (!content.IsWhiteSpace())
            {
                return MarkdownSyntax.IsHeadingLine(content);
            }
        }

        return false;
    }
}
