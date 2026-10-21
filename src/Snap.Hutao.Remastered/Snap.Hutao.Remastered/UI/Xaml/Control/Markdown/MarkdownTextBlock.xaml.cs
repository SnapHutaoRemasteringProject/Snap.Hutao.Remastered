// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

[DependencyProperty<string>("Markdown", PropertyChangedCallbackName = nameof(OnMarkdownChanged))]
internal sealed partial class MarkdownTextBlock : UserControl
{
    private const int PreferredStableChunkLength = 4096;

    private readonly List<MarkdownRenderChunk> stableChunks = [];
    private readonly List<UIElement> tailElements = [];
    private string renderedMarkdown = string.Empty;
    private int stableLength;

    public MarkdownTextBlock()
    {
        InitializeComponent();
        RenderMarkdown(Markdown);
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock view)
        {
            view.RenderMarkdown(e.NewValue as string);
        }
    }

    private void RenderMarkdown(string? markdown)
    {
        string nextMarkdown = markdown ?? string.Empty;

        if (!IsAppendUpdate(nextMarkdown))
        {
            ResetRenderingState();
        }

        RenderAppendUpdate(nextMarkdown);
        renderedMarkdown = nextMarkdown;
    }

    private bool IsAppendUpdate(string markdown)
    {
        return markdown.Length >= renderedMarkdown.Length
            && markdown.StartsWith(renderedMarkdown, StringComparison.Ordinal);
    }

    private void ResetRenderingState()
    {
        stableChunks.Clear();
        ContentHost.Children.Clear();
        renderedMarkdown = string.Empty;
        stableLength = 0;
        tailElements.Clear();
    }

    private void RenderAppendUpdate(string markdown)
    {
        int nextStableLength = MarkdownBlockPartitioner.GetStablePrefixLength(markdown, stableLength);
        if (nextStableLength < stableLength)
        {
            ResetRenderingState();
            nextStableLength = MarkdownBlockPartitioner.GetStablePrefixLength(markdown);
        }

        RemoveTailElements();

        if (nextStableLength > stableLength)
        {
            AppendStableMarkdown(markdown[stableLength..nextStableLength]);
            stableLength = nextStableLength;
        }

        RenderTailMarkdown(markdown[stableLength..]);
    }

    private void AppendStableMarkdown(string markdown)
    {
        if (markdown.Length == 0)
        {
            return;
        }

        if (stableChunks.Count > 0 && !MarkdownBlockPartitioner.StartsWithHeading(markdown))
        {
            MarkdownRenderChunk lastChunk = stableChunks[^1];
            if (lastChunk.Markdown.Length + markdown.Length <= PreferredStableChunkLength)
            {
                UpdateStableChunk(lastChunk, lastChunk.Markdown + markdown);
                return;
            }
        }

        AddStableChunk(markdown);
    }

    private void AddStableChunk(string markdown)
    {
        IReadOnlyList<UIElement> elements = MarkdownHelper.CreateMarkdownBlocks(markdown);
        MarkdownRenderChunk chunk = new(markdown, elements);
        stableChunks.Add(chunk);
        AddContentElements(elements);
    }

    private void UpdateStableChunk(MarkdownRenderChunk chunk, string markdown)
    {
        IReadOnlyList<UIElement> elements = MarkdownHelper.CreateMarkdownBlocks(markdown);
        int index = GetChunkStartIndex(chunk);
        RemoveContentElements(chunk.Elements);
        chunk.Markdown = markdown;
        chunk.Elements = elements;

        InsertContentElements(index, elements);
    }

    private void RenderTailMarkdown(string markdown)
    {
        if (markdown.Length == 0)
        {
            return;
        }

        IReadOnlyList<UIElement> elements = MarkdownHelper.CreateMarkdownBlocks(markdown);
        tailElements.AddRange(elements);
        AddContentElements(elements);
    }

    private void RemoveTailElements()
    {
        if (tailElements.Count == 0)
        {
            return;
        }

        RemoveContentElements(tailElements);
        tailElements.Clear();
    }

    private int GetChunkStartIndex(MarkdownRenderChunk chunk)
    {
        foreach (UIElement element in chunk.Elements)
        {
            int index = ContentHost.Children.IndexOf(element);
            if (index >= 0)
            {
                return index;
            }
        }

        int chunkIndex = stableChunks.IndexOf(chunk);
        for (int i = chunkIndex - 1; i >= 0; i--)
        {
            IReadOnlyList<UIElement> previousElements = stableChunks[i].Elements;
            for (int j = previousElements.Count - 1; j >= 0; j--)
            {
                int previousIndex = ContentHost.Children.IndexOf(previousElements[j]);
                if (previousIndex >= 0)
                {
                    return previousIndex + 1;
                }
            }
        }

        return 0;
    }

    private void AddContentElements(IReadOnlyList<UIElement> elements)
    {
        foreach (UIElement element in elements)
        {
            ContentHost.Children.Add(element);
        }
    }

    private void InsertContentElements(int index, IReadOnlyList<UIElement> elements)
    {
        int insertionIndex = Math.Min(index, ContentHost.Children.Count);
        for (int i = 0; i < elements.Count; i++)
        {
            ContentHost.Children.Insert(insertionIndex + i, elements[i]);
        }
    }

    private void RemoveContentElements(IReadOnlyList<UIElement> elements)
    {
        foreach (UIElement element in elements)
        {
            ContentHost.Children.Remove(element);
        }
    }

    private sealed class MarkdownRenderChunk
    {
        public MarkdownRenderChunk(string markdown, IReadOnlyList<UIElement> elements)
        {
            Markdown = markdown;
            Elements = elements;
        }

        public string Markdown { get; set; }

        public IReadOnlyList<UIElement> Elements { get; set; }
    }
}
