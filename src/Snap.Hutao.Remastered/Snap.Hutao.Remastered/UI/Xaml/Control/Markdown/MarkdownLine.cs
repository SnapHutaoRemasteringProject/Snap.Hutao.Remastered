// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal readonly struct MarkdownLine(int start, int contentEnd, int end)
{
    public int Start { get; } = start;

    public int ContentEnd { get; } = contentEnd;

    public int End { get; } = end;

    public ReadOnlySpan<char> GetContent(ReadOnlySpan<char> markdown)
    {
        return markdown[Start..ContentEnd];
    }
}
