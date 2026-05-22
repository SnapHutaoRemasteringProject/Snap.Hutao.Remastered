// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.Git;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class GitSourcesSpeedTestDialog : ContentDialog
{
    private readonly GitSourcesSpeedTestDialogViewModel viewModel;

    public GitSourcesSpeedTestDialog(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        viewModel = new GitSourcesSpeedTestDialogViewModel(serviceProvider);

        DataContext = viewModel;

        ResultList.ItemsSource = viewModel.TestResults;

        // Subscribe to ViewModel property changes
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(GitSourcesSpeedTestDialogViewModel.IsRunning))
            {
                Progress.Visibility = viewModel.IsRunning ? Visibility.Visible : Visibility.Collapsed;
                RunButton.IsEnabled = !viewModel.IsRunning;
            }
        };

    }
}
