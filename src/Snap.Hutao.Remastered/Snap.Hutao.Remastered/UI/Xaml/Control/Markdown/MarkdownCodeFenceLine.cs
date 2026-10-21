// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal readonly struct MarkdownCodeFenceLine(int fenceLength, int fenceInfoStart)
{
    public int FenceLength { get; } = fenceLength;

    public int FenceInfoStart { get; } = fenceInfoStart;
}
