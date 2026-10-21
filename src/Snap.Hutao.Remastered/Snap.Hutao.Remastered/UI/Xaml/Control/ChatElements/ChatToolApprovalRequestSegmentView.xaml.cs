// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Service.AI.Observables;
using System.Windows.Input;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

[DependencyProperty<ObservableToolApprovalRequestContent>("ToolApprovalRequest")]
[DependencyProperty<ICommand>("ApproveCommand")]
[DependencyProperty<ICommand>("DenyCommand")]
internal sealed partial class ChatToolApprovalRequestSegmentView : UserControl
{
    public ChatToolApprovalRequestSegmentView()
    {
        InitializeComponent();
    }
}
