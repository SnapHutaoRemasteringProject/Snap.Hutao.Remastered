// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal sealed partial class MarkdownCodeBlockView : UserControl
{
    public MarkdownCodeBlockView()
    {
        InitializeComponent();
    }

    public string? Code
    {
        get => (string?)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code),
        typeof(string),
        typeof(MarkdownCodeBlockView),
        new PropertyMetadata(null));

    public string? CodeLanguage
    {
        get => (string?)GetValue(CodeLanguageProperty);
        set => SetValue(CodeLanguageProperty, value);
    }

    public static readonly DependencyProperty CodeLanguageProperty = DependencyProperty.Register(
        nameof(CodeLanguage),
        typeof(string),
        typeof(MarkdownCodeBlockView),
        new PropertyMetadata(null));
}
