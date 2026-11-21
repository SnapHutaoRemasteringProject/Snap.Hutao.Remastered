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

        // The service's IList<T> source can be an ObservableCollection<T>. Keep
        // that concrete generic collection off the WinRT object boundary.
        ArchiveComboBox.ItemsSource = ItemsSourceHelper.Create(archives.Source, false);
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
