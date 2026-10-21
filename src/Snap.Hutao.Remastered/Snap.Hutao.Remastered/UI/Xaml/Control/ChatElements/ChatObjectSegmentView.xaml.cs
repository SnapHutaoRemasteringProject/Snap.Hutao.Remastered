// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

[DependencyProperty<object>("Value")]
internal sealed partial class ChatObjectSegmentView : UserControl
{
    public ChatObjectSegmentView()
    {
        InitializeComponent();
    }
}
