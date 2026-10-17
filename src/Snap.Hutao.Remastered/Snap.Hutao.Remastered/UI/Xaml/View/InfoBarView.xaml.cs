// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Service.Notification;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using WinRT;

namespace Snap.Hutao.Remastered.UI.Xaml.View;

[SuppressMessage("", "CA1001", Justification = "Transition and shutdown subscriptions are released on Unloaded.")]
[DependencyProperty<ObservableCollection<InfoBarOptions>>("InfoBars")]
public sealed partial class InfoBarView : UserControl
{
    private CancellationTokenSource? transitionCancellation;
    private int transitionVersion;
    private DispatcherQueueTimer? clearTimer;
    private Queue<InfoBarOptions>? pendingClear;
    private CancellationTokenRegistration shutdownRegistration;
    private bool viewLoaded;

    public InfoBarView()
    {
        InitializeComponent();
        DataContext = this;
        InfoBars = Ioc.Default.GetRequiredService<IInfoBarService>().Collection;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (XamlApplicationLifetime.Exiting || InfoBars is null) return;
        viewLoaded = true;
        InfoBars.CollectionChanged -= OnInfoBarsCollectionChanged;
        InfoBars.CollectionChanged += OnInfoBarsCollectionChanged;
        WeakReference<InfoBarView> weak = new(this);
        DispatcherQueue dispatcher = DispatcherQueue;
        shutdownRegistration.Dispose();
        shutdownRegistration = XamlApplicationLifetime.ShutdownToken.Register(() =>
        {
            void Stop()
            {
                if (weak.TryGetTarget(out InfoBarView? view) && view.viewLoaded)
                {
                    view.StopClear();
                    view.StopTransition();
                }
            }
            if (dispatcher.HasThreadAccess) Stop();
            else dispatcher.TryEnqueue(Stop);
        });
        bool hasNotifications = InfoBars.Count > 0;
        VisibilityRoot.Visibility = hasNotifications ? Visibility.Visible : Visibility.Collapsed;
        ShowButtonBorder.Visibility = hasNotifications ? Visibility.Collapsed : Visibility.Visible;
        InfoBarItemsBorder.Visibility = hasNotifications ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        viewLoaded = false;
        if (InfoBars is not null) InfoBars.CollectionChanged -= OnInfoBarsCollectionChanged;
        shutdownRegistration.Dispose();
        StopClear();
        StopTransition();
    }

    private void OnInfoBarsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (!viewLoaded || XamlApplicationLifetime.Exiting || InfoBars is null) return;
        if (InfoBars.Count is 0)
            BeginAutomaticTransition(showItems: false);
        else if (args.Action is NotifyCollectionChangedAction.Add)
            BeginAutomaticTransition(showItems: true);
    }

    private void BeginAutomaticTransition(bool showItems)
    {
        if (!viewLoaded || XamlApplicationLifetime.Exiting) return;
        StopTransition();
        FrameworkElement source = showItems ? ShowButtonBorder : InfoBarItemsBorder;
        FrameworkElement target = showItems ? InfoBarItemsBorder : ShowButtonBorder;
        if (source.Visibility is Visibility.Collapsed && target.Visibility is Visibility.Visible)
        {
            VisibilityRoot.Visibility = InfoBars is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        // Only collection-driven transitions are started here. Normal button
        // interactions use EventTriggerBehavior/StartTransitionAction in XAML.
        InfoBarPanelTransitionHelper.Source = source;
        InfoBarPanelTransitionHelper.Target = target;
        InfoBarPanelTransitionHelper.Reset();
        VisibilityRoot.Visibility = Visibility.Visible;
        source.Visibility = target.Visibility = Visibility.Visible;
        VisibilityRoot.UpdateLayout();
        if (!viewLoaded || XamlApplicationLifetime.Exiting) return;
        if (source.ActualWidth <= 0 || target.ActualWidth <= 0)
        {
            source.Visibility = Visibility.Collapsed;
            target.Visibility = Visibility.Visible;
            VisibilityRoot.Visibility = InfoBars is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        transitionCancellation = CancellationTokenSource.CreateLinkedTokenSource(XamlApplicationLifetime.ShutdownToken);
        RunTransitionAsync(source, target, transitionCancellation.Token, transitionVersion).SafeForget();
    }

    private async Task RunTransitionAsync(FrameworkElement source, FrameworkElement target, CancellationToken token, int version)
    {
        DispatcherQueue dispatcher = DispatcherQueue;
        Task? animation = null;
        try
        {
            token.ThrowIfCancellationRequested();
            // Refresh paired elements when the material switched its visual branch.
            animation = InfoBarPanelTransitionHelper.StartAsync(token, forceUpdateAnimatedElements: true);
            await animation.WaitAsync(token).ConfigureAwait(false);
            if (token.IsCancellationRequested || XamlApplicationLifetime.Exiting || version != Volatile.Read(ref transitionVersion)) return;

            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            void Finish()
            {
                try
                {
                    if (!token.IsCancellationRequested && viewLoaded && !XamlApplicationLifetime.Exiting && version == transitionVersion)
                    {
                        CancellationTokenSource? owner = transitionCancellation;
                        transitionCancellation = null;
                        owner?.Dispose();
                        // A declarative button action may have changed direction
                        // while this automatic transition was completing.
                        if (ReferenceEquals(InfoBarPanelTransitionHelper.Source, source)
                            && ReferenceEquals(InfoBarPanelTransitionHelper.Target, target)
                            && InfoBars is { Count: 0 })
                        {
                            VisibilityRoot.Visibility = Visibility.Collapsed;
                        }
                    }
                    completion.TrySetResult();
                }
                catch (Exception exception) { completion.TrySetException(exception); }
            }

            if (dispatcher.HasThreadAccess) Finish();
            else if (!dispatcher.TryEnqueue(Finish)) return;
            await completion.Task.WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Observe any late helper completion without waiting for a native
            // animation callback after the view has already detached.
            animation?.SafeForget();
        }
        catch (COMException exception) when ((!viewLoaded || XamlApplicationLifetime.Exiting || token.IsCancellationRequested)
            && exception.HResult is unchecked((int)0x80004004) or unchecked((int)0x80000013) or unchecked((int)0x8000FFFF))
        {
            System.Diagnostics.Trace.WriteLine($"InfoBar transition already closed: {exception.HResult:X8}");
        }
    }

    private void StopTransition()
    {
        Interlocked.Increment(ref transitionVersion);
        CancellationTokenSource? cancellation = transitionCancellation;
        transitionCancellation = null;
        try
        {
            cancellation?.Cancel();
            InfoBarPanelTransitionHelper.Stop();
            if (InfoBarPanelTransitionHelper.Source is not null && InfoBarPanelTransitionHelper.Target is not null)
                InfoBarPanelTransitionHelper.Reset(toInitialState: false);
        }
        catch (COMException exception) when ((!viewLoaded || XamlApplicationLifetime.Exiting)
            && exception.HResult is unchecked((int)0x80004004) or unchecked((int)0x80000013) or unchecked((int)0x8000FFFF))
        {
            System.Diagnostics.Trace.WriteLine($"InfoBar transition already closed: {exception.HResult:X8}");
        }
        finally { cancellation?.Dispose(); }
    }

    private void OnInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        if (viewLoaded && !XamlApplicationLifetime.Exiting)
            InfoBars?.Remove(sender.DataContext.As<InfoBarOptions>());
    }

    private void OnClearAllButtonClick(object sender, RoutedEventArgs e)
    {
        if (!viewLoaded || XamlApplicationLifetime.Exiting || InfoBars is not { Count: > 0 } infoBars) return;
        StopClear();
        pendingClear = new(infoBars.Reverse());
        clearTimer = DispatcherQueue.CreateTimer();
        clearTimer.Interval = TimeSpan.FromMilliseconds(50);
        clearTimer.IsRepeating = true;
        clearTimer.Tick += OnClearTick;
        clearTimer.Start();
        RemoveNextInfoBar();
    }

    private void OnClearTick(DispatcherQueueTimer sender, object args)
    {
        if (!ReferenceEquals(sender, clearTimer)) return;
        RemoveNextInfoBar();
    }

    private void RemoveNextInfoBar()
    {
        Queue<InfoBarOptions>? clearing = pendingClear;
        if (!viewLoaded || XamlApplicationLifetime.Exiting || clearing is not { Count: > 0 })
        {
            StopClear();
            return;
        }
        InfoBars?.Remove(clearing.Dequeue());
        if (ReferenceEquals(pendingClear, clearing) && clearing.Count is 0) StopClear();
    }

    private void StopClear()
    {
        DispatcherQueueTimer? timer = clearTimer;
        clearTimer = null;
        pendingClear = null;
        if (timer is null) return;
        try
        {
            timer.Tick -= OnClearTick;
            timer.Stop();
        }
        catch (COMException exception) when ((!viewLoaded || XamlApplicationLifetime.Exiting)
            && exception.HResult is unchecked((int)0x80004004) or unchecked((int)0x80000013) or unchecked((int)0x8000FFFF))
        {
            System.Diagnostics.Trace.WriteLine($"InfoBar clear timer already closed: {exception.HResult:X8}");
        }
    }
}
