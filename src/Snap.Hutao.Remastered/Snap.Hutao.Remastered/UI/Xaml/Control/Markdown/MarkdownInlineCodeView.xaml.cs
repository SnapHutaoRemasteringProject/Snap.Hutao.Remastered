// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Markdown;

[DependencyProperty<string>("Text")]
internal sealed partial class MarkdownInlineCodeView : UserControl
{
    public MarkdownInlineCodeView()
    {
        InitializeComponent();
    }

}
