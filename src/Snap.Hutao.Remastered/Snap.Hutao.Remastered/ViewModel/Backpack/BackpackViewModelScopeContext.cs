// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Backpack;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Yae;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

[Service(ServiceLifetime.Scoped)]
public sealed partial class BackpackViewModelScopeContext
{
    [GeneratedConstructor]
    public partial BackpackViewModelScopeContext(IServiceProvider serviceProvider);

    public partial IBackpackService BackpackService { get; }

    public partial IContentDialogFactory ContentDialogFactory { get; }

    public partial IMessenger Messenger { get; }

    public partial IMetadataService MetadataService { get; }

    public partial IServiceProvider ServiceProvider { get; }

    public partial ITaskContext TaskContext { get; }

    public partial IYaeService YaeService { get; }
}
