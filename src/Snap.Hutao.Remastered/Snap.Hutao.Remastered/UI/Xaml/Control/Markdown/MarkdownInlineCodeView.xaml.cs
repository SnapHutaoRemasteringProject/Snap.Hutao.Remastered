// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

internal sealed partial class MarkdownInlineCodeView : UserControl
{
    public MarkdownInlineCodeView()
    {
        InitializeComponent();
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MarkdownInlineCodeView),
        new PropertyMetadata(null));
}
