// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using JetBrains.Annotations;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.Factory.ContentDialog;

public interface IContentDialogFactory
{
    bool IsDialogShowing { get; }

    ITaskContext TaskContext { get; }

    ValueTask<ContentDialogResult> CreateForConfirmAsync([LocalizationRequired] string title, [LocalizationRequired] string content);

    ValueTask<ContentDialogResult> CreateForConfirmCancelAsync([LocalizationRequired] string title, [LocalizationRequired] string content, ContentDialogButton defaultButton = ContentDialogButton.Close, bool isPrimaryButtonEnabled = true);

    ValueTask<ContentDialogResult> CreateForConfirmCancelWithSecondaryAsync([LocalizationRequired] string title, [LocalizationRequired] string content, [LocalizationRequired] string secondaryButtonText, ContentDialogButton defaultButton = ContentDialogButton.Primary);

    ValueTask<Microsoft.UI.Xaml.Controls.ContentDialog> CreateForIndeterminateProgressAsync([LocalizationRequired] string title);

    ValueTask<TContentDialog> CreateInstanceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContentDialog>(IServiceProvider serviceProvider, params object[] parameters)
        where TContentDialog : Microsoft.UI.Xaml.Controls.ContentDialog;

    ValueContentDialogTask EnqueueAndShowAsync(Microsoft.UI.Xaml.Controls.ContentDialog contentDialog);
}
