// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

[DependencyProperty<string>("Message")]
[DependencyProperty<InfoBarSeverity>("Severity", NotNull = true)]
public sealed partial class CountdownConfirmDialog : ContentDialog
{
    private const int CountdownSeconds = 5;

    private readonly IContentDialogFactory contentDialogFactory;
    private DispatcherQueueTimer? countdownTimer;
    private int remainingSeconds = CountdownSeconds;

    [GeneratedConstructor(InitializeComponent = true)]
    public partial CountdownConfirmDialog(IServiceProvider serviceProvider);

    public CountdownConfirmDialog(IServiceProvider serviceProvider, string title, string message, InfoBarSeverity severity)
        : this(serviceProvider)
    {
        Title = title;
        Message = message;
        Severity = severity;

        IsPrimaryButtonEnabled = false;
        UpdatePrimaryButtonText();
        Opened += OnDialogOpened;
        Closed += OnDialogClosed;
        Unloaded += OnDialogUnloaded;
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
        StopCountdown();
        remainingSeconds = CountdownSeconds;
        IsPrimaryButtonEnabled = false;
        UpdatePrimaryButtonText();

        countdownTimer = DispatcherQueue.CreateTimer();
        countdownTimer.Interval = TimeSpan.FromSeconds(1);
        countdownTimer.Tick += OnCountdownTimerTick;
        countdownTimer.Start();
    }

    private void OnCountdownTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (countdownTimer is null)
        {
            return;
        }

        if (--remainingSeconds <= 0)
        {
            StopCountdown();
            IsPrimaryButtonEnabled = true;
            PrimaryButtonText = SH.ViewDialogFastSkipTalkConfirmPrimaryButtonText;
            return;
        }

        UpdatePrimaryButtonText();
    }

    private void OnDialogClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        StopCountdown();
    }

    private void OnDialogUnloaded(object sender, RoutedEventArgs args)
    {
        StopCountdown();
    }

    private void StopCountdown()
    {
        if (countdownTimer is not { } timer)
        {
            return;
        }

        countdownTimer = null;
        timer.Stop();
        timer.Tick -= OnCountdownTimerTick;
    }

    private void UpdatePrimaryButtonText()
    {
        PrimaryButtonText = SH.FormatViewDialogCountdownConfirmCountdownSuffix(SH.ViewDialogFastSkipTalkConfirmPrimaryButtonText, remainingSeconds);
    }
}
