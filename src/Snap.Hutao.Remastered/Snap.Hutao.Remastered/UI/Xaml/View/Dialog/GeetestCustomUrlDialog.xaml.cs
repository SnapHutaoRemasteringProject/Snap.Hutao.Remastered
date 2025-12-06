// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("Text")]
internal sealed partial class GeetestCustomUrlDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    public GeetestCustomUrlDialog(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        Text = serviceProvider.GetRequiredService<AppOptions>().GeetestCustomCompositeUrl.Value;
        contentDialogFactory = serviceProvider.GetRequiredService<IContentDialogFactory>();
    }

    public async ValueTask<ValueResult<bool, string>> GetUrlAsync()
    {
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        return new(result is ContentDialogResult.Primary, Text ?? string.Empty);
    }
}