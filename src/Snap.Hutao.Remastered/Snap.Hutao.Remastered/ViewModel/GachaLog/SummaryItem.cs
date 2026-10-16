// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

[WinRT.GeneratedBindableCustomProperty]
[ObservableObject]
public sealed partial class SummaryItem : Item
{
    [ObservableProperty]
    public partial bool IsUp { get; set; }

    [ObservableProperty]
    public partial bool IsGuarantee { get; set; }

    // Used in ListView for ProgressBar Maximum Value
    [ObservableProperty]
    public partial int GuaranteeOrangeThreshold { get; set; }

    [ObservableProperty]
    public partial int LastPull { get; set; }

    // Total pulls since the last rate-up (up) 5-star, combining both lost-50/50 and guarantee pity.
    // Only meaningful for IsUp == true items.
    [ObservableProperty]
    public partial int TotalCyclePull { get; set; }

    public string FormattedTime
    {
        get => $"{Time.ToLocalTime():yyy.MM.dd HH:mm:ss}";
    }

    [ObservableProperty]
    public partial Windows.UI.Color Color { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTime))]
    public partial DateTimeOffset Time { get; set; }
}
