// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Snap.Hutao.Remastered.Core.Text.Json.Converter;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Common.Announcement;

public sealed partial class Announcement : AnnouncementContent
{
    #region Binding

    public bool ShouldShowTimeDescription
    {
        get => Type == 1;
    }

    public Brush TimeDescriptionBrush
    {
        get
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan span = EndTime - now;

            if (StartTime > now)
            {
                return new SolidColorBrush(Colors.Blue);
            }
            else if (span.TotalDays <= 10)
            {
                return new SolidColorBrush(Colors.Red);
            }

            return new SolidColorBrush(Colors.White);
        }
    }

    public string TimeDescription
    {
        get
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // 尚未开始
            if (StartTime > now)
            {
                TimeSpan span = StartTime - now;
                return span.TotalDays <= 1 ? SH.FormatWebAnnouncementTimeHoursBegin((int)span.TotalHours) : SH.FormatWebAnnouncementTimeDaysBegin((int)span.TotalDays);
            }
            else
            {
                TimeSpan span = EndTime - now;
                return span.TotalDays <= 1 ? SH.FormatWebAnnouncementTimeHoursEnd((int)span.TotalHours) : SH.FormatWebAnnouncementTimeDaysEnd((int)span.TotalDays);
            }
        }
    }

    public bool ShouldShowTimePercent
    {
        get => ShouldShowTimeDescription && TimePercent is > 0 and < 1;
    }

    public double TimePercent
    {
        get
        {
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            TimeSpan current = currentTime - StartTime;
            TimeSpan total = EndTime - StartTime;
            return current / total;
        }
    }

    public string TimeFormatted
    {
        get => $"{StartTime.ToLocalTime():yyyy.MM.dd HH:mm} - {EndTime.ToLocalTime():yyyy.MM.dd HH:mm}";
    }
    #endregion

    [JsonPropertyName("type_label")]
    [ObservableProperty]
    public partial string TypeLabel { get; set; } = default!;

    [JsonPropertyName("tag_label")]
    [ObservableProperty]
    public partial string TagLabel { get; set; } = default!;

    [JsonPropertyName("tag_icon")]
    [ObservableProperty]
    public partial string TagIcon { get; set; } = default!;

    [JsonPropertyName("login_alert")]
    [ObservableProperty]
    public partial int LoginAlert { get; set; }

    [JsonPropertyName("start_time")]
    [JsonConverter(typeof(SimpleDateTimeOffsetConverter))]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeDescriptionBrush))]
    [NotifyPropertyChangedFor(nameof(TimeDescription))]
    [NotifyPropertyChangedFor(nameof(TimePercent))]
    [NotifyPropertyChangedFor(nameof(ShouldShowTimePercent))]
    [NotifyPropertyChangedFor(nameof(TimeFormatted))]
    public partial DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(SimpleDateTimeOffsetConverter))]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeDescriptionBrush))]
    [NotifyPropertyChangedFor(nameof(TimeDescription))]
    [NotifyPropertyChangedFor(nameof(TimePercent))]
    [NotifyPropertyChangedFor(nameof(ShouldShowTimePercent))]
    [NotifyPropertyChangedFor(nameof(TimeFormatted))]
    public partial DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("type")]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShouldShowTimeDescription))]
    [NotifyPropertyChangedFor(nameof(ShouldShowTimePercent))]
    public partial int Type { get; set; }

    [JsonPropertyName("remind")]
    [ObservableProperty]
    public partial int Remind { get; set; }

    [JsonPropertyName("alert")]
    [ObservableProperty]
    public partial int Alert { get; set; }

    [JsonPropertyName("tag_start_time")]
    [ObservableProperty]
    public partial string TagStartTime { get; set; } = default!;

    [JsonPropertyName("tag_end_time")]
    [ObservableProperty]
    public partial string TagEndTime { get; set; } = default!;

    [JsonPropertyName("remind_ver")]
    [ObservableProperty]
    public partial int RemindVersion { get; set; }

    [JsonPropertyName("has_content")]
    [ObservableProperty]
    public partial bool HasContent { get; set; }
}
