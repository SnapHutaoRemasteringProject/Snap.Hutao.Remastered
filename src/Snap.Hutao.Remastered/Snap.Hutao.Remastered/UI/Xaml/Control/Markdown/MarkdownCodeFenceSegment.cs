// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal readonly ref struct MarkdownCodeFenceSegment(int fenceLength, ReadOnlySpan<char> fenceInfo)
{
    public int FenceLength { get; } = fenceLength;

    public ReadOnlySpan<char> FenceInfo { get; } = fenceInfo;
}
