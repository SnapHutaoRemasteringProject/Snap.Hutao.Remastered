// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle;
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
            if (currentWindowReference.Window is null)
            {
                return false;
            }

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

    public ValueWindowedContentDialogTask EnqueueAndShowAsync(WinUI3Package.WindowedContentDialog windowedContentDialog)
    {
        TaskCompletionSource queueSource = new();
        TaskCompletionSource<Microsoft.UI.Xaml.Controls.ContentDialogResult> resultSource = new();

        PrivateEnqueueAndShowAsync(windowedContentDialog, queueSource, resultSource).SafeForget();
        return new(queueSource.Task, resultSource.Task);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::WinUI3Package.WindowedContentDialog))]
    private async Task PrivateEnqueueAndShowAsync(WinUI3Package.WindowedContentDialog windowedContentDialog, TaskCompletionSource queueSource, TaskCompletionSource<Microsoft.UI.Xaml.Controls.ContentDialogResult> resultSource)
    {
        using (await dialogShowLock.LockAsync().ConfigureAwait(false))
        {
            await taskContext.SwitchToMainThreadAsync();
            queueSource.TrySetResult();

            IAsyncOperation<Microsoft.UI.Xaml.Controls.ContentDialogResult> operation = windowedContentDialog.ShowAsync();
            resultSource.SetResult(await operation);
        }
    }
}
