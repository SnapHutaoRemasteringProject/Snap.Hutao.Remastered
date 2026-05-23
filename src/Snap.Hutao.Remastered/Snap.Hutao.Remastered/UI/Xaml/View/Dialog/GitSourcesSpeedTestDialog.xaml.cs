// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.ViewModel.Git;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class GitSourcesSpeedTestDialog : ContentDialog
{
    private readonly GitSourcesSpeedTestDialogViewModel viewModel;

    private readonly IContentDialogFactory contentDialogFactory;

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

    //public async ValueTask<ValueResult<bool, string?>> GetInputUrlAsync()
    //{
    //    ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
    //    await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
    //    return new(result is ContentDialogResult.Primary, Text);
    //}
}
