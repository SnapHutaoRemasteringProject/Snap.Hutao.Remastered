// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DataTransfer;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Service.Achievement;
using Snap.Hutao.Remastered.Service.Metadata;

namespace Snap.Hutao.Remastered.ViewModel.Achievement;

[Service(ServiceLifetime.Scoped)]
internal sealed partial class AchievementViewModelScopeContext
{
    [GeneratedConstructor]
    public partial AchievementViewModelScopeContext(IServiceProvider serviceProvider);

    public partial IFileSystemPickerInteraction FileSystemPickerInteraction { get; }

    public partial ILogger<AchievementViewModelScopeContext> Logger { get; }

    public partial JsonSerializerOptions JsonSerializerOptions { get; }

    public partial IContentDialogFactory ContentDialogFactory { get; }

    public partial AchievementImporter AchievementImporter { get; }

    public partial IAchievementService AchievementService { get; }

    public partial IClipboardProvider ClipboardProvider { get; }

    public partial IServiceProvider ServiceProvider { get; }

    public partial IMetadataService MetadataService { get; }

    public partial ITaskContext TaskContext { get; }

    public partial IMessenger Messenger { get; }
}