// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

internal sealed partial class LastWindowCloseBehaviorConfigurationDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial LastWindowCloseBehaviorConfigurationDialog(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, LastWindowCloseBehavior>> GetLastWindowCloseBehaviorAsync()
    {
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        return new(result is ContentDialogResult.Primary, (LastWindowCloseBehavior)CloseButtonBehaviorSelector.SelectedIndex);
    }
}