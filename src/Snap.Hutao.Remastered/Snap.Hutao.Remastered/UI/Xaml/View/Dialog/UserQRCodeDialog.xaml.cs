// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.WindowedContentDialog;
using Snap.Hutao.Remastered.Factory.QuickResponse;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Response;
using System.IO;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
public sealed partial class UserQRCodeDialog : ContentDialog, IDisposable
{
    private readonly HoyoPlayPassportClient hoyoPlayPassportClient;
    private readonly IWindowedContentDialogFactory windowedContentDialogFactory;
    private readonly IQRCodeFactory qrCodeFactory;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;

    private readonly CancellationTokenSource userManualCancellationTokenSource = new();
    private bool disposed;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial UserQRCodeDialog(IServiceProvider serviceProvider);

    ~UserQRCodeDialog()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            userManualCancellationTokenSource.Dispose();
            disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask<ValueResult<bool, QrLoginResult?>> GetQrLoginResultAsync()
    {
        // The wrapper, not the main window, will set the ContentDialog's XamlRoot.
        await taskContext.SwitchToMainThreadAsync();
        ValueContentDialogTask dialogTask = windowedContentDialogFactory.EnqueueAndShowAsync(this);
        bool started = false;

        try
        {
            await dialogTask.QueueTask.ConfigureAwait(false);
            started = true;

            // Closing the native window also closes its hosted ContentDialog.
            // Race the login loop against ShowTask so external window closure cancels polling.
            Task<ValueResult<bool, QrLoginResult?>> loginTask = PrivateGetQRLoginResultAsync().AsTask();
            if (await Task.WhenAny(loginTask, dialogTask.ShowTask).ConfigureAwait(false) == dialogTask.ShowTask)
            {
                userManualCancellationTokenSource.Cancel();
            }

            return await loginTask.ConfigureAwait(false);
        }
        finally
        {
            userManualCancellationTokenSource.Cancel();
            if (started)
            {
                await taskContext.SwitchToMainThreadAsync();
                if (!dialogTask.ShowTask.IsCompleted)
                {
                    Hide();
                }

                // Do not dispose the login scope before the host window is gone.
                await dialogTask.ShowTask.ConfigureAwait(false);
            }
        }
    }

    [Command("CancelCommand")]
    private void Cancel()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Cancel", "UserQRCodeDialog.Command"));
        userManualCancellationTokenSource.Cancel();
    }

    private async ValueTask<ValueResult<bool, QrLoginResult?>> PrivateGetQRLoginResultAsync()
    {
        while (!userManualCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                CancellationToken token = userManualCancellationTokenSource.Token;
                string ticket = await FetchQRCodeAndSetImageAsync(token).ConfigureAwait(false);
                QrLoginResult? uidGameToken = await WaitQueryQRCodeConfirmAsync(ticket, token).ConfigureAwait(false);

                if (uidGameToken is null)
                {
                    continue;
                }

                return new(true, uidGameToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return new(false, default);
    }

    private async ValueTask<string> FetchQRCodeAndSetImageAsync(CancellationToken token)
    {
        Response<QrLogin> qrLoginResponse = await hoyoPlayPassportClient.CreateQrLoginAsync(token).ConfigureAwait(false);
        if (!ResponseValidator.TryValidate(qrLoginResponse, messenger, out QrLogin? qrLogin))
        {
            return string.Empty;
        }

        await taskContext.SwitchToMainThreadAsync();

        BitmapImage bitmap = new();
        await bitmap.SetSourceAsync(new MemoryStream(qrCodeFactory.Create(qrLogin.Url)).AsRandomAccessStream());
        QRCodeSource = bitmap;

        return qrLogin.Ticket;
    }

    private async ValueTask<QrLoginResult?> WaitQueryQRCodeConfirmAsync(string ticket, CancellationToken token)
    {
        using (PeriodicTimer timer = new(TimeSpan.FromSeconds(3)))
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Response<QrLoginResult> query = await hoyoPlayPassportClient.QueryQrLoginStatusAsync(ticket, token).ConfigureAwait(false);

                if (query is { ReturnCode: 0, Data: { Status: "Confirmed", Tokens: [{ TokenType: 1 }] } })
                {
                    return query.Data;
                }

                if (query.ReturnCode is (int)KnownReturnCode.QRLoginExpired)
                {
                    break;
                }
            }
        }

        return null;
    }
}