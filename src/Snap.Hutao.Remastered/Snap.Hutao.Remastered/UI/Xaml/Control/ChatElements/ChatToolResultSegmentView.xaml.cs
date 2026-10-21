// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Service.AI.Observables;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

[DependencyProperty<ObservableToolResultContent>("ToolResult")]
internal sealed partial class ChatToolResultSegmentView : UserControl
{
    public ChatToolResultSegmentView()
    {
        InitializeComponent();
    }
}
