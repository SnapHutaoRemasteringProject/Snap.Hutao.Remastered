// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("Text")]
public sealed partial class BackpackArchiveCreateDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial BackpackArchiveCreateDialog(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, string>> GetInputAsync()
    {
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        return new(result is ContentDialogResult.Primary, Text ?? string.Empty);
    }
}
