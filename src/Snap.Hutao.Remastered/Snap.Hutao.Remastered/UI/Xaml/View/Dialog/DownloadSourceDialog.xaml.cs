// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Update;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class DownloadSourceDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial DownloadSourceDialog(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, DownloadSourceKind>> GetDownloadSourceAsync(bool offerPackaged)
    {
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        if (!offerPackaged)
        {
            DownloadSourceSelector.Items.Remove(PackagedOption);
            OfficialDescription.Text = SH.ViewDialogDownloadSourceOfficialPackagedDescription;
        }

        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        return new(result is ContentDialogResult.Primary, (DownloadSourceKind)DownloadSourceSelector.SelectedIndex);
    }
}
