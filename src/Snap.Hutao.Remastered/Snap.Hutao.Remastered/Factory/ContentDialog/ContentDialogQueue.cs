// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Windows.Foundation;

namespace Snap.Hutao.Remastered.Factory.ContentDialog;

[Service(ServiceLifetime.Singleton, typeof(IContentDialogQueue))]
[SuppressMessage("", "SH003")]
[SuppressMessage("", "SH100")]
[SuppressMessage("", "RS0030")]
public sealed partial class ContentDialogQueue : IContentDialogQueue
{
    private readonly SemaphoreSlim dialogShowLock = new(1, 1);

    private readonly ICurrentXamlWindowReference currentWindowReference;
    private readonly ITaskContext taskContext;

    [GeneratedConstructor]
    public partial ContentDialogQueue(IServiceProvider serviceProvider);

    public bool IsDialogShowing
    {
        get
        {
            if (currentWindowReference.Window is null)
            {
                return false;
            }

            return dialogShowLock.CurrentCount == 0;
        }
    }

    public ValueContentDialogTask EnqueueAndShowAsync(Microsoft.UI.Xaml.Controls.ContentDialog contentDialog, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource queueSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ContentDialogResult> resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        PrivateEnqueueAndShowAsync(contentDialog, queueSource, resultSource, cancellationToken).SafeForget();
        return new(queueSource.Task, resultSource.Task);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.FrameworkElement))]
    private async Task PrivateEnqueueAndShowAsync(Microsoft.UI.Xaml.Controls.ContentDialog contentDialog, TaskCompletionSource queueSource, TaskCompletionSource<ContentDialogResult> resultSource, CancellationToken cancellationToken)
    {
        try
        {
            await dialogShowLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await taskContext.SwitchToMainThreadAsync();
                cancellationToken.ThrowIfCancellationRequested();

                XamlRoot root = contentDialog.XamlRoot ?? throw HutaoException.NotSupported("Dialog created without XamlRoot");

                if (root != currentWindowReference.XamlRoot)
                {
                    HutaoException.NotSupported("Dialog using different XamlRoot");
                }

                // Queued and open dialogs use the current host theme.
                if (root.Content is FrameworkElement element)
                {
                    contentDialog.SetBinding(FrameworkElement.RequestedThemeProperty, new Binding
                    {
                        Source = element,
                        Path = new PropertyPath(nameof(FrameworkElement.RequestedTheme)),
                        Mode = BindingMode.OneWay,
                    });
                }

                IAsyncOperation<ContentDialogResult> operation = contentDialog.ShowAsync();
                queueSource.TrySetResult();
                contentDialog.Focus(FocusState.Programmatic);
                resultSource.TrySetResult(await operation);
            }
            finally
            {
                dialogShowLock.Release();
            }
        }
        catch (OperationCanceledException cancelled)
        {
            queueSource.TrySetCanceled(cancelled.CancellationToken);
            resultSource.TrySetCanceled(cancelled.CancellationToken);
        }
        catch (Exception error)
        {
            queueSource.TrySetException(error);
            resultSource.TrySetException(error);
        }
    }
}
