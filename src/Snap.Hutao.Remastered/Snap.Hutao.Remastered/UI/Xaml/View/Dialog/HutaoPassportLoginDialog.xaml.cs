// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("UserName")]
[DependencyProperty<string>("Password")]
[DependencyProperty<string>("PassportUserNameHint")]
public sealed partial class HutaoPassportLoginDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial HutaoPassportLoginDialog(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, (string? UserName, string? Passport)>> GetInputAsync(string passportUserNameHint, string? userName = default)
    {
        UserName = userName;
        PassportUserNameHint = passportUserNameHint;
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        (string? resultUserName, string? resultPassword) = (UserName, Password);
        UserName = string.Empty;
        Password = string.Empty;
        return new(result is ContentDialogResult.Primary, (resultUserName, resultPassword));
    }
}