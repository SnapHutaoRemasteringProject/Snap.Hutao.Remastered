// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Game.Scheme;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<IEnumerable<LaunchScheme>>("KnownSchemes")]
[DependencyProperty<LaunchScheme>("SelectedScheme")]
internal sealed partial class LaunchGameConfigurationFixDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial LaunchGameConfigurationFixDialog(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, LaunchScheme?>> GetLaunchSchemeAsync()
    {
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        return new(result is ContentDialogResult.Primary, SelectedScheme);
    }
}