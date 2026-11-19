// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Factory.WindowedContentDialog;

public readonly struct ValueWindowedContentDialogTask
{
    public readonly Task QueueTask;

    public readonly Task<Microsoft.UI.Xaml.Controls.ContentDialogResult> ShowTask;

    public ValueWindowedContentDialogTask(Task queueTask, Task<Microsoft.UI.Xaml.Controls.ContentDialogResult> showTask)
    {
        QueueTask = queueTask;
        ShowTask = showTask;
    }
}
