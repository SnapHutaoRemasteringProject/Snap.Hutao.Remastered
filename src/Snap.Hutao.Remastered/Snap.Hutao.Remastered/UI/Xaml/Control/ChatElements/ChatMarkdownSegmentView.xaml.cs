// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

[DependencyProperty<string>("Title")]
[DependencyProperty<string>("Markdown")]
[DependencyProperty<double>("MarkdownOpacity", DefaultValue = 1.0, NotNull = true)]
internal sealed partial class ChatMarkdownSegmentView : UserControl
{
    public ChatMarkdownSegmentView()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void CopyMarkdown()
    {
        string? markdown = Markdown;
        if (string.IsNullOrEmpty(markdown))
        {
            return;
        }

        DataPackage dataPackage = new();
        dataPackage.SetText(markdown);
        Clipboard.SetContent(dataPackage);
    }
}
