// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.UI.Xaml;
using Snap.Hutao.Remastered.Win32.Foundation;

namespace Snap.Hutao.Remastered.ViewModel.Abstraction;

public abstract partial class ViewModel : ObservableObject, IViewModel, IDisposable
{
    private readonly object initializationGate = new();
    private readonly DispatcherQueue? viewDispatcher = DispatcherQueue.GetForCurrentThread();
    private Task? loadingTask;

    [ObservableProperty]
    public partial bool IsInitialized { get; protected set; }

    public CancellationToken CancellationToken { get; set; }

    [field: MaybeNull]
    public SemaphoreSlim CriticalSection { get => field ??= new(1); private set; }

    public IDeferContentLoader? DeferContentLoader { get; set; }

    [field: MaybeNull]
    public IProperty<bool> IsViewUnloaded { get => field ??= Property.Create(false); protected set; }

    protected TaskCompletionSource<bool> Initialization { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public virtual void Dispose()
    {
        CriticalSection.Dispose();
    }

    public void Resurrect()
    {
        lock (initializationGate)
        {
            Initialization.TrySetResult(false);
            Initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
            loadingTask = null;
            IsViewUnloaded.Value = false;
        }
    }

    public void Uninitialize()
    {
        try
        {
            IsInitialized = false;
        }
        catch (Exception ex) when (ex.HResult is HRESULT.E_UNEXPECTED)
        {
            // Ignore
        }

        // Must be called before setting IsViewUnloaded to true, we sometime set property value to null in it
        // BindableCustomPropertyProvider will check IsViewUnloaded before SetProperty
        UninitializeOverride();

        IsViewUnloaded.Value = true;
        lock (initializationGate) Initialization.TrySetResult(false);
        DeferContentLoader = default;
    }

    [Command("LoadCommand")]
    protected virtual Task LoadAsync()
    {
        lock (initializationGate)
        {
            if (IsInitialized || XamlApplicationLifetime.Exiting || IsViewUnloaded.Value) return Task.CompletedTask;
            if (loadingTask is { IsCompleted: false }) return loadingTask;
            if (Initialization.Task.IsCompleted)
                Initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);

            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            loadingTask = completion.Task;
            // Publish before invoking the override, which can synchronously re-enter.
            RunLoadAsync(completion, Initialization, CancellationToken).SafeForget();
            return completion.Task;
        }
    }

    private async Task RunLoadAsync(TaskCompletionSource completion, TaskCompletionSource<bool> initialization, CancellationToken token)
    {
        try
        {
            bool initialized = await LoadOverrideAsync(token).ConfigureAwait(false);
            await PublishInitializationAsync(initialized, initialization, token).ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            completion.TrySetCanceled();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            initialization.TrySetResult(false);
        }
    }

    // Retries must publish the same notification as the initial load. Updating
    // metadata alone cannot re-enable a page whose first load was canceled.
    protected Task PublishInitializationAsync(bool initialized, CancellationToken token)
    {
        return PublishInitializationAsync(initialized, Initialization, token);
    }

    private async Task PublishInitializationAsync(bool initialized, TaskCompletionSource<bool> initialization, CancellationToken token)
    {
        if (XamlApplicationLifetime.Exiting || token.IsCancellationRequested || IsViewUnloaded.Value) return;
        DispatcherQueue dispatcher = viewDispatcher
            ?? throw new InvalidOperationException("View models must be created on their UI dispatcher.");
        void Publish()
        {
            // An old load must not publish into a resurrected view's new generation.
            if (XamlApplicationLifetime.Exiting || token.IsCancellationRequested || IsViewUnloaded.Value
                || !ReferenceEquals(initialization, Initialization)) return;
            IsInitialized = initialized;
            initialization.TrySetResult(initialized);
        }

        if (dispatcher.HasThreadAccess)
        {
            Publish();
            return;
        }

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        if (!dispatcher.TryEnqueue(() =>
        {
            try { Publish(); completion.TrySetResult(); }
            catch (Exception exception) { completion.TrySetException(exception); }
        })) throw new OperationCanceledException("The view dispatcher has shut down.");
        // Queue shutdown must release the load even if the callback never runs.
        await completion.Task.WaitAsync(linked.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Override this method to implement the loading logic.
    /// </summary>
    /// <param name="token">The same as <see cref="CancellationToken"/></param>
    /// <returns>Is the initialization successful</returns>
    protected virtual ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return ValueTask.FromResult(true);
    }

    protected virtual void UninitializeOverride()
    {
    }

    protected async ValueTask<IDisposable> EnterCriticalSectionAsync()
    {
        ThrowIfViewDisposed();
        IDisposable disposable = await CriticalSection.EnterAsync(CancellationToken).ConfigureAwait(false);
        ThrowIfViewDisposed();
        return disposable;
    }

    protected void MakeSubViewModel(ReadOnlySpan<ViewModel> subViewModels)
    {
        foreach (ViewModel subViewModel in subViewModels)
        {
            subViewModel.CancellationToken = CancellationToken;
            subViewModel.CriticalSection = CriticalSection;
            subViewModel.IsViewUnloaded = IsViewUnloaded;
        }
    }

    private void ThrowIfViewDisposed()
    {
        if (IsViewUnloaded.Value)
        {
            HutaoException.OperationCanceled(SH.ViewModelViewDisposedOperationCancel);
        }
    }
}
