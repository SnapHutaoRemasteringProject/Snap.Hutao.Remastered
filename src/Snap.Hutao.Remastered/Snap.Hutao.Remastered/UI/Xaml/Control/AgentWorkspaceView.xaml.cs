// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.AI;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

[DependencyProperty<AgentViewModel>("ViewModel", PropertyChangedCallbackName = nameof(OnViewModelChanged))]
internal sealed partial class AgentWorkspaceView : UserControl
{
    public AgentWorkspaceView() { InitializeComponent(); }

    private static void OnViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        AgentWorkspaceView view = (AgentWorkspaceView)sender;
        view.DataContext = args.NewValue;
        view.Bindings.Update();
    }
}
