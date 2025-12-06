// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DataTransfer;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.AvatarInfo;
using Snap.Hutao.Remastered.Service.Cultivation;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.User;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

[Service(ServiceLifetime.Scoped)]
internal sealed partial class AvatarPropertyViewModelScopeContext
{
    [GeneratedConstructor]
    public partial AvatarPropertyViewModelScopeContext(IServiceProvider serviceProvider);

    public partial IContentDialogFactory ContentDialogFactory { get; }

    public partial IServiceScopeFactory ServiceScopeFactory { get; }

    public partial ICultivationService CultivationService { get; }

    public partial IAvatarInfoService AvatarInfoService { get; }

    public partial IClipboardProvider ClipboardProvider { get; }

    public partial IServiceProvider ServiceProvider { get; }

    public partial IMetadataService MetadataService { get; }

    public partial ITaskContext TaskContext { get; }

    public partial IUserService UserService { get; }

    public partial IMessenger Messenger { get; }
}