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

    public async ValueTask<ValueResult<bool, DownloadSourceKind>> GetDownloadSourceAsync(bool offerPackaged, bool offerOfficial = true)
    {
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        if (!offerPackaged)
        {
            DownloadSourceSelector.Items.Remove(PackagedOption);
            OfficialDescription.Text = SH.ViewDialogDownloadSourceOfficialPackagedDescription;
        }

        if (!offerOfficial)
        {
            DownloadSourceSelector.Items.Remove(OfficialOption);
            DownloadSourceSelector.SelectedIndex = 0;
        }

        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        object? selected = DownloadSourceSelector.SelectedItem;
        DownloadSourceKind source = ReferenceEquals(selected, OfficialOption) ? DownloadSourceKind.Official
            : ReferenceEquals(selected, PackagedOption) ? DownloadSourceKind.AppInstaller
            : DownloadSourceKind.MirrorChyan;
        return new(result is ContentDialogResult.Primary && selected is not null, source);
    }
}
