// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Factory.ContentDialog;

namespace Snap.Hutao.Remastered.Factory.WindowedContentDialog;

public interface IWindowedContentDialogFactory
{
    ITaskContext TaskContext { get; }

    ValueTask<TDialog> CreateInstanceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDialog>(
        IServiceProvider serviceProvider, params object[] parameters)
        where TDialog : Microsoft.UI.Xaml.Controls.ContentDialog;

    public ValueWindowedContentDialogTask EnqueueAndShowAsync(WinUI3Package.WindowedContentDialog windowedContentDialog);
}
