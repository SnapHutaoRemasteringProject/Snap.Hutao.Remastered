// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.Guide;

namespace Snap.Hutao.Remastered.UI.Xaml.View;

public sealed partial class GuideView : UserControl
{
    public GuideView()
    {
        InitializeComponent();
    }

    private void OnGuideProgressLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Segmented.OnApplyTemplate restores its first observed index. Apply the
        // current state after that template reset; x:Bind observes subsequent State changes.
        if (sender is CommunityToolkit.WinUI.Controls.Segmented progress && DataContext is GuideViewModel viewModel)
            progress.SelectedIndex = GuideNavigation.StepIndex(viewModel.State);
    }

    private void OnControlMaterialSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox selector && DataContext is GuideViewModel viewModel)
            viewModel.SelectControlMaterial(selector.SelectedIndex);
    }
}