// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.Web.Hutao.HutaoAsAService;

public sealed partial class Announcement : UploadAnnouncement
{
    [ObservableProperty]
    public partial long Id { get; set; }

    [ObservableProperty]
    public partial string Locale { get; set; } = default!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateTimeFormatted))]
    public partial long LastUpdateTime { get; set; }

    public string UpdateTimeFormatted { get => $"{DateTimeOffset.FromUnixTimeSeconds(LastUpdateTime).ToLocalTime():yyyy.MM.dd HH:mm:ss}"; }

    [ObservableProperty]
    public partial ICommand? DismissCommand { get; set; }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Severity)) OnPropertyChanged(nameof(CanDismiss));
    }

    public bool CanDismiss { get => Severity <= InfoBarSeverity.Success; }
}
