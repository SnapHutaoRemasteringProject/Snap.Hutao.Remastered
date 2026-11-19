// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Windows.Foundation;

namespace Snap.Hutao.Remastered.Factory.WindowedContentDialog;

[Service(ServiceLifetime.Singleton, typeof(IWindowedContentDialogQueue))]
public sealed partial class WindowedContentDialogQueue : IWindowedContentDialogQueue
{
    private readonly AsyncLock dialogShowLock = new();

    private readonly ICurrentXamlWindowReference currentWindowReference;
    private readonly ITaskContext taskContext;

    [GeneratedConstructor]
    public partial WindowedContentDialogQueue(IServiceProvider serviceProvider);

    public bool IsDialogShowing
    {
        get
        {
            if (dialogShowLock.TryLock(out AsyncLock.Releaser releaser))
            {
                using (releaser)
                {
                }

                return false;
            }

            return true;
        }
    }

    public ValueContentDialogTask EnqueueAndShowAsync(ContentDialog contentDialog)
    {
        TaskCompletionSource queueSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ContentDialogResult> resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        PrivateEnqueueAndShowAsync(contentDialog, queueSource, resultSource).SafeForget();
        return new(queueSource.Task, resultSource.Task);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::WinUI3Package.WindowedContentDialog))]
    private async Task PrivateEnqueueAndShowAsync(ContentDialog contentDialog, TaskCompletionSource queueSource, TaskCompletionSource<ContentDialogResult> resultSource)
    {
        void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args) => queueSource.TrySetResult();

        try
        {
            using (await dialogShowLock.LockAsync().ConfigureAwait(false))
            {
                await taskContext.SwitchToMainThreadAsync();

                // An independent WindowedContentDialog owns this XAML ContentDialog.
                // Never attach the hosted dialog to the parent window's XamlRoot.
                WinUI3Package.WindowedContentDialog windowedDialog = new(contentDialog);
                contentDialog.Opened += OnOpened;

                try
                {
                    IAsyncOperation<ContentDialogResult> operation = currentWindowReference.Window is { } parentWindow
                        ? windowedDialog.ShowAsync(parentWindow)
                        : windowedDialog.ShowAsync();

                    ContentDialogResult result = await operation;
                    if (!queueSource.Task.IsCompleted)
                    {
                        queueSource.TrySetException(new InvalidOperationException("The windowed dialog closed before its content was opened."));
                    }

                    resultSource.TrySetResult(result);
                }
                finally
                {
                    await taskContext.SwitchToMainThreadAsync();
                    contentDialog.Opened -= OnOpened;
                }
            }
        }
        catch (Exception exception)
        {
            // Both awaiters must finish even when native window creation or ShowAsync fails.
            queueSource.TrySetException(exception);
            resultSource.TrySetException(exception);
        }
    }
}
