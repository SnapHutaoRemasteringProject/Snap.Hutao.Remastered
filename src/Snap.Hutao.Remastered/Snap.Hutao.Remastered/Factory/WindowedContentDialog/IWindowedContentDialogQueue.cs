// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.Factory.WindowedContentDialog;

public interface IWindowedContentDialogQueue
{
    bool IsDialogShowing { get; }

    ValueContentDialogTask EnqueueAndShowAsync(Microsoft.UI.Xaml.Controls.ContentDialog contentDialog);
}
