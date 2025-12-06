// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("UserName")]
[DependencyProperty<string>("Password")]
[DependencyProperty<string>("VerifyCode")]
internal sealed partial class HutaoPassportUnregisterDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IMessenger messenger;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial HutaoPassportUnregisterDialog(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult<bool, (string? UserName, string? Passport, string? VerifyCode)>> GetInputAsync(string? userName)
    {
        InitializeUserNameTextBox(userName);
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        return new(result is ContentDialogResult.Primary, (UserName, Password, VerifyCode));
    }

    [Command("VerifyCommand")]
    private async Task VerifyAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Verify", "HutaoPassportUnregisterDialog.Command"));

        if (string.IsNullOrEmpty(UserName))
        {
            return;
        }

        if (!UserName.IsEmail())
        {
            messenger.Send(InfoBarMessage.Warning(SH.ViewModelHutaoPassportEmailNotValidHint));
            return;
        }

        using (IServiceScope scope = serviceScopeFactory.CreateScope())
        {
            HutaoPassportClient hutaoPassportClient = scope.ServiceProvider.GetRequiredService<HutaoPassportClient>();

            HutaoResponse response = await hutaoPassportClient.RequestVerifyAsync(UserName, VerifyCodeRequestType.CancelRegistration).ConfigureAwait(false);
            if (!ResponseValidator.TryValidate(response, scope.ServiceProvider))
            {
                await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
                Hide();
                return;
            }

            messenger.Send(InfoBarMessage.Information(response.GetLocalizationMessage()));
        }
    }

    private void InitializeUserNameTextBox(string? userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            return;
        }

        UserName = userName;
        UserNameTextBox.IsEnabled = false;
    }
}