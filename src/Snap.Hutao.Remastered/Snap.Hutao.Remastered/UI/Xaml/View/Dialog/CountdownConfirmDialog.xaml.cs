// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("Message")]
[DependencyProperty<string>("ConfirmText")]
public sealed partial class CountdownConfirmDialog : ContentDialog
{
    private const int CountdownSeconds = 5;

    private readonly IContentDialogFactory contentDialogFactory;
    private DispatcherQueueTimer? countdownTimer;
    private int remainingSeconds = CountdownSeconds;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial CountdownConfirmDialog(IServiceProvider serviceProvider);

    public CountdownConfirmDialog(IServiceProvider serviceProvider, string title, string message, string confirmText)
        : this(serviceProvider)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
    }

    public async ValueTask<bool> ConfirmAsync()
    {
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
        StartCountdown();
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        return result is ContentDialogResult.Primary;
    }

    private void StartCountdown()
    {
        remainingSeconds = CountdownSeconds;
        IsPrimaryButtonEnabled = false;
        DefaultButton = ContentDialogButton.None;
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
            DefaultButton = ContentDialogButton.Primary;
            PrimaryButtonText = ConfirmText;
            return;
        }

        UpdatePrimaryButtonText();
    }

    private void UpdatePrimaryButtonText()
    {
        PrimaryButtonText = string.Format(SH.ViewDialogCountdownConfirmCountdownFormat, ConfirmText, remainingSeconds);
    }
}
