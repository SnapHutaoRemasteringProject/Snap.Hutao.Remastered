// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Factory.WindowedContentDialog;

[Service(ServiceLifetime.Singleton, typeof(IWindowedContentDialogFactory))]
public sealed partial class WindowedContentDialogFactory : IWindowedContentDialogFactory
{
    private readonly IWindowedContentDialogQueue windowedContentDialogQueue;
    public partial ITaskContext TaskContext { get; }


    [GeneratedConstructor]
    public partial WindowedContentDialogFactory(IServiceProvider serviceProvider);

    public async ValueTask<TDialog> CreateInstanceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDialog>(
        IServiceProvider serviceProvider, params object[] parameters)
        where TDialog : Microsoft.UI.Xaml.Controls.ContentDialog
    {
        await TaskContext.SwitchToMainThreadAsync();

        return ActivatorUtilities.CreateInstance<TDialog>(serviceProvider, parameters);
    }

    public ValueWindowedContentDialogTask EnqueueAndShowAsync(WinUI3Package.WindowedContentDialog windowedContentDialog)
    {
        return windowedContentDialogQueue.EnqueueAndShowAsync(windowedContentDialog);
    }
}
