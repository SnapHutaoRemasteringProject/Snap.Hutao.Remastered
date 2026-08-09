// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Backpack;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class BackpackArchivePickerDialog : ContentDialog
{
    private readonly IBackpackService backpackService;
    private readonly IContentDialogFactory contentDialogFactory;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial BackpackArchivePickerDialog(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, BackpackArchive>> GetSelectedArchiveAsync()
    {
        IAdvancedDbCollectionView<BackpackArchive> archives = await backpackService.GetArchiveCollectionAsync().ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();

        ArchiveComboBox.ItemsSource = archives.Source;
        if (archives.Source.Count > 0)
        {
            ArchiveComboBox.SelectedIndex = 0;
        }

        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();

        return result is ContentDialogResult.Primary
            ? new(true, (ArchiveComboBox.SelectedItem as BackpackArchive)!)
            : new(false, default!);
    }
}
