// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public abstract partial class Wish : ObservableObject
{
    public required string Name { get; init => SetProperty(ref field, value); }

    public required int TotalCount { get; init => SetProperty(ref field, value); }

    public string FormattedTimeSpan
    {
        get
        {
            if (From == DateTimeOffset.MaxValue && To == DateTimeOffset.MinValue)
            {
                return string.Empty;
            }

            return $"{From:yyyy.MM.dd} - {To:yyyy.MM.dd}";
        }
    }

    public string FormattedTotalCount
    {
        get => SH.FormatModelBindingGachaWishBaseTotalCount(TotalCount);
    }

    public required DateTimeOffset From { get; init => SetProperty(ref field, value); }

    public required DateTimeOffset To { get; init => SetProperty(ref field, value); }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(From):
                OnPropertyChanged(nameof(FormattedTimeSpan));
                break;
            case nameof(To):
                OnPropertyChanged(nameof(FormattedTimeSpan));
                break;
            case nameof(TotalCount):
                OnPropertyChanged(nameof(FormattedTotalCount));
                break;
        }
    }
}
