// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.UI.Input;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

[SuppressMessage("", "CA1001")]
[DependencyProperty<TimeSpan>("Period", NotNull = true)]
[DependencyProperty<ICommand>("Command")]
[DependencyProperty<object>("CommandParameter")]
public sealed partial class PeriodicInvokeCommandOrOnActualThemeChangedBehavior : BehaviorBase<FrameworkElement>
{
    private PeriodicCommandSignal? currentRun;

    protected override bool Initialize()
    {
        AssociatedObject.ActualThemeChanged += OnActualThemeChanged;
        return true;
    }

    protected override bool Uninitialize()
    {
        currentRun?.Dispose();
        currentRun = null;
        AssociatedObject.ActualThemeChanged -= OnActualThemeChanged;

        return true;
    }

    protected override void OnAssociatedObjectLoaded()
    {
        if (XamlApplicationLifetime.Exiting) return;
        currentRun?.Dispose();
        PeriodicCommandSignal run = new(Period);
        currentRun = run;
        PrivateRunAsync(run, AssociatedObject.DispatcherQueue).SafeForget();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        currentRun?.NotifyThemeChanged();
    }

    private async Task ExecuteCommandAsync(PeriodicCommandSignal run)
    {
        if (XamlApplicationLifetime.Exiting || !ReferenceEquals(currentRun, run) || !IsAttached || AssociatedObject is not { IsLoaded: true })
        {
            return;
        }

        if (Command is IAsyncRelayCommand asyncCommand)
        {
            if (asyncCommand.CanExecute(CommandParameter)) await asyncCommand.ExecuteAsync(CommandParameter).ConfigureAwait(false);
        }
        else
        {
            Command.TryExecute(CommandParameter);
        }
    }

    [SuppressMessage("", "SH003")]
    private async Task PrivateRunAsync(PeriodicCommandSignal run, DispatcherQueue dispatcher)
    {
        using (run)
        {
            while (await run.WaitAsync().ConfigureAwait(false))
            {
                if (XamlApplicationLifetime.Exiting) break;
                if (!dispatcher.TryEnqueue(() => ExecuteCommandAsync(run).SafeForget())) break;
            }
        }
    }
}
