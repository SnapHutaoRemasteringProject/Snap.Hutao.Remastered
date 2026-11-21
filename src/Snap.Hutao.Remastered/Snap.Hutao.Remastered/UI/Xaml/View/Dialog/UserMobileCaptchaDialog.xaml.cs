// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Geetest;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Response;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Windows.System;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class UserMobileCaptchaDialog : ContentDialog, IPassportMobileCaptchaProvider, INotifyPropertyChanged
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly IGeetestService geetestService;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial UserMobileCaptchaDialog(IServiceProvider serviceProvider);

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Mobile
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                IsSendCaptchaEnabled = MobilePhoneRegex.IsMatch(value);
                OnPropertyChanged(nameof(IsSendCaptchaEnabled));
            }
        }
    }

    public bool IsSendCaptchaEnabled { get; private set; }

    public string? Captcha
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsLoginEnabled = !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(ActionType);
                OnPropertyChanged(nameof(IsLoginEnabled));
            }
        }
    }

    public bool IsLoginEnabled { get; private set; }

    public string? ActionType { get; private set; }

    public string? Aigis { get; set; }

    [GeneratedRegex(@"\d{11}")]
    private static partial Regex MobilePhoneRegex { get; }

    [Command("SendMobileCaptchaCommand")]
    public async Task SendMobileCaptchaAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Send captcha", "UserMobileCaptchaDialog.Command"));

        string mobile = Mobile ?? throw new ArgumentNullException(nameof(Mobile));
        Aigis = null;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IPassportClient passportClient = scope.ServiceProvider.GetRequiredService<IOverseaSupportFactory<IPassportClient>>().Create(false);
            (string? rawSession, Response<MobileCaptcha> response) = await passportClient.CreateLoginCaptchaAsync(mobile, null).ConfigureAwait(false);
            string? previousSession = null;
            for (int attempt = 0; attempt < 2 && response.ReturnCode is not 0 && !string.IsNullOrWhiteSpace(rawSession); attempt++)
            {
                if (string.Equals(rawSession, previousSession, StringComparison.Ordinal)
                    || !await geetestService.TryVerifyAigisSessionAsync(this, rawSession, false).ConfigureAwait(false)
                    || string.IsNullOrWhiteSpace(Aigis))
                {
                    break;
                }

                previousSession = rawSession;
                (rawSession, response) = await passportClient.CreateLoginCaptchaAsync(mobile, Aigis).ConfigureAwait(false);
            }

            bool hasChallenge = !string.IsNullOrWhiteSpace(rawSession);
            System.Diagnostics.Debug.WriteLine($"Passport SMS: retcode={response.ReturnCode}, challenge header present={hasChallenge}");
            if ((KnownReturnCode)response.ReturnCode is KnownReturnCode.RET_NEED_AIGIS && !hasChallenge)
            {
                response.Message = $"{response.Message}\n{SH.ViewModelUserSmsChallengeUnavailable}";
            }

            try
            {
                if (ResponseValidator.TryValidate(response, scope.ServiceProvider, out MobileCaptcha? mobileCaptcha))
                {
                    ActionType = mobileCaptcha.ActionType;
                }
            }
            catch (ObjectDisposedException)
            {
                // The user close the dialog before network request finished
                return;
            }
        }

        // Prevent re-enable too soon, and user might not receive the short message
        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
    }

    public async ValueTask<bool> GetMobileCaptchaAsync()
    {
        return await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false) is ContentDialogResult.Primary;
    }

    private void OnTextKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Enter)
        {
            e.Handled = true;
        }
    }

    private void OnTextKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Enter)
        {
            e.Handled = true;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        return false;
    }
}
