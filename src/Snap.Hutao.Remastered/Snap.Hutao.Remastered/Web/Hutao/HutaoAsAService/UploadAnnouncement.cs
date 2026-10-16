// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.Web.Hutao.HutaoAsAService;

public partial class UploadAnnouncement : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = default!;

    [ObservableProperty]
    public partial string Content { get; set; } = default!;

    [ObservableProperty]
    public partial InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial string Link { get; set; } = default!;

    [ObservableProperty]
    public partial string? MaxPresentVersion { get; set; }
}
