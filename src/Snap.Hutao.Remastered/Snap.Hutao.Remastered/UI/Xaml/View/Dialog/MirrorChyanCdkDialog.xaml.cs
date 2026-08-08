// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("Text")]
public sealed partial class MirrorChyanCdkDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    public MirrorChyanCdkDialog(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        Text = LocalSetting.Get(SettingKeys.MirrorChyanCdk, string.Empty);
        contentDialogFactory = serviceProvider.GetRequiredService<IContentDialogFactory>();
    }

    public async ValueTask<ValueResult<bool, string>> GetCdkAsync()
    {
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        return new(result is ContentDialogResult.Primary, Text ?? string.Empty);
    }
}
