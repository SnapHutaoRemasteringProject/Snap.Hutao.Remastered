// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public sealed partial class Countdown : ObservableObject
{
    public Countdown(Item item)
    {
        Item = item;
        Histories.CollectionChanged += OnHistoriesChanged;
    }

    public string FormattedLastTime
    {
        get => LastTime <= DateTimeOffset.Now ? SH.FormatViewModelGachaLogCountdownLastTime(LastTime) : SH.ViewModelGachaLogCountdownCurrentWish;
    }

    public string FormattedVersionOrder { get => Histories.FirstOrDefault()?.FormattedVersionOrder ?? string.Empty; }

    public string FormattedCountdown
    {
        get
        {
            int cdDays = (int)(DateTimeOffset.Now - LastTime).TotalDays;
            return cdDays > 0 ? SH.FormatViewModelGachaLogCountdownLastTimeDelta(cdDays) : SH.FormatViewModelGachaLogCountdownCurrentWishDelta(-cdDays);
        }
    }

    public string FormattedHistoryCount { get => SH.FormatViewModelGachaLogCountdownHistoryCount(Histories.Count); }

    public Item Item { get; }

    public ObservableCollection<CountdownHistory> Histories { get; } = [];

    private void OnHistoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LastTime));
        OnPropertyChanged(nameof(FormattedLastTime));
        OnPropertyChanged(nameof(FormattedVersionOrder));
        OnPropertyChanged(nameof(FormattedCountdown));
        OnPropertyChanged(nameof(FormattedHistoryCount));
    }

    public DateTimeOffset LastTime { get => Histories.FirstOrDefault()?.LastTime ?? default; }
}
