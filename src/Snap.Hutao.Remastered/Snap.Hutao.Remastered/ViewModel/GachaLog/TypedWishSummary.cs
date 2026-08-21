// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public sealed partial class TypedWishSummary : Wish, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public required string TypeName { get; init; }

    public string FormattedMaxOrangePull
    {
        get => SH.FormatModelBindingGachaTypedWishSummaryMaxOrangePull(MaxOrangePull);
    }

    public string FormattedMinOrangePull
    {
        get => SH.FormatModelBindingGachaTypedWishSummaryMinOrangePull(MinOrangePull);
    }

    public required int LastOrangePull { get; init; }

    public required int LastPurplePull { get; init; }

    public required int LastBluePull { get; init; }

    public required int GuaranteeOrangeThreshold { get; init; }

    public required int GuaranteePurpleThreshold { get; init; }

    public required int GuaranteeBlueThreshold { get; init; }

    public string FormattedTotalOrange
    {
        get => $"{TotalOrangePull} [{(TotalOrangePercent is double.NaN ? 0D : TotalOrangePercent),6:p2}]";
    }

    public string FormattedTotalPurple
    {
        get => $"{TotalPurplePull} [{(TotalPurplePercent is double.NaN ? 0D : TotalPurplePercent),6:p2}]";
    }

    public string FormattedTotalBlue
    {
        get => $"{TotalBluePull} [{(TotalBluePercent is double.NaN ? 0D : TotalBluePercent),6:p2}]";
    }

    public string FormattedTotalOther
    {
        get => $"{TotalOtherPull} [{(TotalOtherPercent is double.NaN ? 0D : TotalOtherPercent),6:p2}]";
    }

    public string FormattedAverageOrangePull
    {
        get => SH.FormatModelBindingGachaTypedWishSummaryAveragePull(AverageOrangePull);
    }

    public bool IsPredictPullAvailable { get; set => SetProperty(ref field, value); }

    public string FormattedAverageUpOrangePull
    {
        get => SH.FormatModelBindingGachaTypedWishSummaryAveragePull(AverageUpOrangePull);
    }

    public string FormattedAveragePurplePull
    {
        get => SH.FormatModelBindingGachaTypedWishSummaryAveragePull(AveragePurplePull);
    }

    public string FormattedPredictedPullLeftToOrange
    {
        get => SH.FormatViewModelGachaLogPredictedPullLeftToOrange(PredictedPullLeftToOrange, ProbabilityOfPredictedPullLeftToOrange);
    }

    public string FormattedProbabilityOfNextPullIsOrange
    {
        get => SH.FormatViewModelGachaLogProbabilityOfNextPullIsOrange(ProbabilityOfNextPullIsOrange);
    }

    public required List<SummaryItem> SummaryList { get; init; }

    public bool ShowCombinedTotal
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(DisplayList));
            }
        }
    }

    public List<SummaryItem> DisplayList
    {
        get
        {
            if (!ShowCombinedTotal)
            {
                return SummaryList;
            }

            return BuildCombinedView();
        }
    }

    private List<SummaryItem> BuildCombinedView()
    {
        List<SummaryItem> result = [];
        bool isFirst = true;

        foreach (SummaryItem item in SummaryList)
        {
            if (item.IsUp)
            {
                // Clone with LastPull = TotalCyclePull to display the combined total
                // Double the threshold so the progress bar and color gradient work at 180 scale
                result.Add(new()
                {
                    Id = item.Id,
                    Name = item.Name,
                    Icon = item.Icon,
                    Badge = item.Badge,
                    Quality = item.Quality,
                    IsUp = item.IsUp,
                    IsGuarantee = item.IsGuarantee,
                    GuaranteeOrangeThreshold = item.GuaranteeOrangeThreshold * 2,
                    LastPull = item.TotalCyclePull,
                    TotalCyclePull = item.TotalCyclePull,
                    Color = item.Color,
                    Time = item.Time,
                });
            }
            else if (isFirst)
            {
                // Keep the most recent non-up item (unresolved loss)
                result.Add(item);
            }

            isFirst = false;
        }

        return result;
    }

    public required int MaxOrangePull { get; init; }

    public required int MinOrangePull { get; init; }

    public required int TotalOrangePull { get; init; }

    public required double TotalOrangePercent { get; init; }

    public required int TotalPurplePull { get; init; }

    public required double TotalPurplePercent { get; init; }

    public required int TotalBluePull { get; init; }

    public required double TotalBluePercent { get; init; }

    public int TotalOtherPull => TotalCount - TotalOrangePull - TotalPurplePull - TotalBluePull;

    public double TotalOtherPercent => TotalCount == 0 ? double.NaN : (1.0 - TotalOrangePercent - TotalPurplePercent - TotalBluePercent);

    public required double AverageOrangePull { get; init; }

    public required double AverageUpOrangePull { get; init; }

    public required double AveragePurplePull { get; init; }

    public int PredictedPullLeftToOrange
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(FormattedPredictedPullLeftToOrange));
        }
    }

    public double ProbabilityOfPredictedPullLeftToOrange
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(FormattedPredictedPullLeftToOrange));
        }
    }

    public double ProbabilityOfNextPullIsOrange
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(FormattedProbabilityOfNextPullIsOrange));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        return false;
    }

}
