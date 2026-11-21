// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.UI.Input;
using System.Diagnostics;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

[UsedImplicitly]
[DependencyProperty<ICommand>("Command", PropertyChangedCallbackName = nameof(OnCommandChanged))]
[DependencyProperty<object>("CommandParameter")]
public sealed partial class InvokeCommandOnLoadedBehavior : BehaviorBase<UIElement>
{
    private ICommand? executedCommand;

    protected override void OnAttached()
    {
        base.OnAttached();
        TraceLifecycle("Attached");

        // FrameworkElement in a ItemsRepeater gets attached twice
        if (AssociatedObject is FrameworkElement { IsLoaded: true })
        {
            TryExecuteCommand();
        }
    }

    protected override void OnAssociatedObjectLoaded()
    {
        TraceLifecycle("Loaded");
        TryExecuteCommand();
    }

    private static void OnCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        InvokeCommandOnLoadedBehavior behavior = (InvokeCommandOnLoadedBehavior)sender;
        behavior.TraceLifecycle("Command changed");
        if (behavior.AssociatedObject is not FrameworkElement { IsLoaded: true } element)
        {
            return;
        }

        // x:Bind may finish after attachment/loading. Defer until parameters from
        // the same binding update have arrived, without retaining an unloaded view.
        WeakReference<InvokeCommandOnLoadedBehavior> weak = new(behavior);
        element.DispatcherQueue.TryEnqueue(() =>
        {
            if (weak.TryGetTarget(out InvokeCommandOnLoadedBehavior? target)
                && target.AssociatedObject is FrameworkElement { IsLoaded: true })
            {
                target.TryExecuteCommand();
            }
        });
    }

    private void TryExecuteCommand()
    {
        TraceLifecycle("Checking command");
        if (XamlApplicationLifetime.Exiting || AssociatedObject is not FrameworkElement { IsLoaded: true })
        {
            return;
        }

        if (ReferenceEquals(executedCommand, Command))
        {
            return;
        }

        if (Command is IAsyncRelayCommand asyncCommand)
        {
            if (asyncCommand.CanExecute(CommandParameter))
            {
                executedCommand = Command;
                TraceLifecycle("Executing async command");
                // Execute() uses async-void exception dispatch back to XAML.
                // Observe the task so cancellation during unloading is handled.
                asyncCommand.ExecuteAsync(CommandParameter).SafeForget();
            }
        }
        else
        {
            if (Command.TryExecute(CommandParameter)) executedCommand = Command;
        }
    }

    [Conditional("DEBUG")]
    private void TraceLifecycle(string stage)
    {
        Debug.WriteLine($"[InvokeCommandOnLoadedBehavior] {stage}; Host={AssociatedObject?.GetType().FullName ?? "<detached>"}; Loaded={(AssociatedObject as FrameworkElement)?.IsLoaded}; Command={Command?.GetType().FullName ?? "<null>"}");
    }
}
