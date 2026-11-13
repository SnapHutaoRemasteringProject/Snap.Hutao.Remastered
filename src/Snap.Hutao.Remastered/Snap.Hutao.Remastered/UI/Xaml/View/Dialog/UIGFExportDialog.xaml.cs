// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using System.Collections.Immutable;
using WinRT;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<List<UIGFUidSelection>>("ItemsSource")]
public sealed partial class UIGFExportDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;
    private ImmutableArray<uint> selectedUids = [];

    public UIGFExportDialog(IServiceProvider serviceProvider, ImmutableArray<uint> uids, bool isLegacyVersion = false)
    {
        InitializeComponent();
        contentDialogFactory = serviceProvider.GetRequiredService<IContentDialogFactory>();

        if (isLegacyVersion)
        {
            SelectionListView.SelectionMode = ListViewSelectionMode.Single;
        }

        ItemsSource = uids.Select(UIGFUidSelection.Create).ToList();
        IsPrimaryButtonEnabled = false;
    }

    public async ValueTask<ValueResult<bool, ImmutableArray<uint>>> GetSelectedUidsAsync()
    {
        if (await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false) is ContentDialogResult.Primary)
        {
            await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
            return new(true, selectedUids);
        }

        return new(false, selectedUids);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        selectedUids = UIGFUidSelection.GetSelectedUidArray(sender.As<ListViewBase>());
        IsPrimaryButtonEnabled = selectedUids.Length > 0;
    }
}
