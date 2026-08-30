// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("Message")]
[DependencyProperty<string>("ConfirmText")]
[DependencyProperty<InfoBarSeverity>("Severity", NotNull = true)]
public sealed partial class CountdownConfirmDialog : ContentDialog
{
    private const int CountdownSeconds = 5;

    private readonly IContentDialogFactory contentDialogFactory;
    private DispatcherQueueTimer? countdownTimer;
    private int remainingSeconds = CountdownSeconds;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial CountdownConfirmDialog(IServiceProvider serviceProvider);

    public CountdownConfirmDialog(IServiceProvider serviceProvider, string title, string message, string confirmText, InfoBarSeverity severity)
        : this(serviceProvider)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        Severity = severity;

        IsPrimaryButtonEnabled = false;
        PrimaryButtonText = SH.FormatViewDialogCountdownConfirmCountdownSuffix(confirmText, CountdownSeconds);
        Opened += OnDialogOpened;
    }

    public async ValueTask<bool> ConfirmAsync()
    {
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        return result is ContentDialogResult.Primary;
    }

    private void OnDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        StartCountdown();
    }

    private void StartCountdown()
    {
        remainingSeconds = CountdownSeconds;
        IsPrimaryButtonEnabled = false;
        UpdatePrimaryButtonText();

        countdownTimer ??= DispatcherQueue.CreateTimer();
        countdownTimer.Interval = TimeSpan.FromSeconds(1);
        countdownTimer.Tick -= OnCountdownTimerTick;
        countdownTimer.Tick += OnCountdownTimerTick;
        countdownTimer.Stop();
        countdownTimer.Start();
    }

    private void OnCountdownTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (--remainingSeconds <= 0)
        {
            countdownTimer?.Stop();
            IsPrimaryButtonEnabled = true;
            PrimaryButtonText = ConfirmText;
            return;
        }

        UpdatePrimaryButtonText();
    }

    private void UpdatePrimaryButtonText()
    {
        PrimaryButtonText = SH.FormatViewDialogCountdownConfirmCountdownSuffix(ConfirmText, remainingSeconds);
    }
}
