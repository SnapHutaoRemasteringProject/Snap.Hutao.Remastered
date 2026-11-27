// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Factory.ContentDialog;

[SuppressMessage("", "SH003")]
public interface IContentDialogQueue
{
    bool IsDialogShowing { get; }

    ValueContentDialogTask EnqueueAndShowAsync(Microsoft.UI.Xaml.Controls.ContentDialog contentDialog, CancellationToken cancellationToken = default);
}
