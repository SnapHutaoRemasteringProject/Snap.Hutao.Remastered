// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.DataTransfer;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.Notification;

namespace Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay.Connect;

public partial class PackageSegment : ObservableObject
{
    [JsonPropertyName("url")]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string Url { get; set; } = default!;

    [JsonPropertyName("md5")]
    [ObservableProperty]
    public partial string MD5 { get; set; } = default!;

    [JsonPropertyName("size")]
    [ObservableProperty]
    public partial long Size { get; set; } = default!;

    [JsonPropertyName("decompressed_size")]
    [ObservableProperty]
    public partial long DecompressedSize { get; set; } = default!;

    [JsonIgnore]
    public string DisplayName { get => System.IO.Path.GetFileName(Url); }

    [Command("CopyPathCommand")]
    private async Task CopyPathToClipboardAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Copy url to ClipBoard", "PackageSegment.Command"));

        IServiceProvider serviceProvider = Ioc.Default;
        await serviceProvider.GetRequiredService<IClipboardProvider>().SetTextAsync(Url).ConfigureAwait(false);
        serviceProvider.GetRequiredService<IMessenger>().Send(InfoBarMessage.Success(SH.WebGameResourcePathCopySucceed));
    }
}
