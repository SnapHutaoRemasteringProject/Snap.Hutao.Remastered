// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.UI.Xaml.Data.Converter.Specialized;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.HardChallenge;

public sealed partial class DataEntryView : ObservableObject, IPropertyValuesProvider
{
    private DataEntryView(bool singlePlayer, HardChallengeDataEntry dataEntry, HardChallengeMetadataContext context)
    {
        Name = singlePlayer
            ? SH.ViewModelHardChalllengeDataEntrySinglePlayerName
            : SH.ViewModelHardChalllengeDataEntryMultiPlayerName;
        Debug.Assert(dataEntry.HasData);
        DifficultyIcon = HardChallengeDifficultyIconConverter.Convert(dataEntry.Best.Icon.Split(',').Last());
        Difficulty = dataEntry.Best.Difficulty.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture);
        FormattedSeconds = SH.FormatViewModelHardChallengeSeconds(dataEntry.Best.Seconds);

        Challenges = dataEntry.Challenges.SelectAsArray(ChallengeView.Create, context);
    }

    public string Name { get; private set => SetProperty(ref field, value); }

    public Uri DifficultyIcon { get; private set => SetProperty(ref field, value); }

    public string? Difficulty { get; private set => SetProperty(ref field, value); }

    public string FormattedSeconds { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<ChallengeView> Challenges { get; private set => SetProperty(ref field, value); }

    public static DataEntryView? Create(bool singlePlayer, HardChallengeDataEntry dataEntry, HardChallengeMetadataContext context)
    {
        if (!dataEntry.HasData)
        {
            return default;
        }

        return new(singlePlayer, dataEntry, context);
    }
}
