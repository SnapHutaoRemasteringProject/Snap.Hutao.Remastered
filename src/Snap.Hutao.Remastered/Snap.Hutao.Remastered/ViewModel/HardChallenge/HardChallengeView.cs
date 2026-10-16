// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using MetadataHardChallengeSchedule = Snap.Hutao.Remastered.Model.Metadata.HardChallengeSchedule;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;
using System.Collections.Immutable;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.HardChallenge;

public sealed partial class HardChallengeView : ObservableObject, IEntityAccess<HardChallengeEntry?>, IPropertyValuesProvider
{
    private HardChallengeView(HardChallengeEntry entity, HardChallengeMetadataContext context)
        : this(context.IdHardChallengeScheduleMap[entity.ScheduleId], context)
    {
        Entity = entity;

        HardChallengeData hardChallengeData = entity.HardChallengeData;

        ImmutableArray<DataEntryView>.Builder builder = ImmutableArray.CreateBuilder<DataEntryView>(2);

        if (DataEntryView.Create(true, hardChallengeData.SinglePlayer, context) is { } singlePlayer)
        {
            builder.Add(singlePlayer);
        }

        if (DataEntryView.Create(false, hardChallengeData.MultiPlayer, context) is { } multiPlayer)
        {
            builder.Add(multiPlayer);
        }

        DataEntries = builder.ToImmutable().AsAdvancedCollectionView();

        HardChallengeDifficultyLevel maxDifficulty = default;
        if (hardChallengeData.SinglePlayer.HasData)
        {
            maxDifficulty = hardChallengeData.SinglePlayer.Best.Difficulty;
        }

        if (hardChallengeData.MultiPlayer.HasData && hardChallengeData.MultiPlayer.Best.Difficulty > maxDifficulty)
        {
            maxDifficulty = hardChallengeData.MultiPlayer.Best.Difficulty;
        }

        if (maxDifficulty > HardChallengeDifficultyLevel.None)
        {
            MaxDifficulty = maxDifficulty.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture);
        }

        BlingAvatars = hardChallengeData.Blings.SelectAsArray(AvatarBling.Create, context);
        Engaged = true;
    }

    private HardChallengeView(MetadataHardChallengeSchedule hardChallengeSchedule, HardChallengeMetadataContext context)
    {
        ScheduleId = hardChallengeSchedule.Id;
        ScheduleName = hardChallengeSchedule.Name;
        FormattedTime = $"{hardChallengeSchedule.Begin:yyyy.MM.dd HH:mm} - {hardChallengeSchedule.End:yyyy.MM.dd HH:mm}";
    }

    public uint ScheduleId { get; private set => SetProperty(ref field, value); }

    public string ScheduleName { get; private set => SetProperty(ref field, value); }

    public string Schedule { get => SH.FormatModelEntityHardChallengeSchedule(ScheduleId - 5269000, ScheduleName); }

    public string FormattedTime { get; private set => SetProperty(ref field, value); }

    public string? MaxDifficulty { get; private set => SetProperty(ref field, value); }

    public bool Engaged { get; private set => SetProperty(ref field, value); }

    public HardChallengeEntry? Entity { get; private set => SetProperty(ref field, value); }

    public IAdvancedCollectionView<DataEntryView>? DataEntries { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<AvatarBling> BlingAvatars { get; private set => SetProperty(ref field, value); } = [];

    public static HardChallengeView Create(HardChallengeEntry entity, HardChallengeMetadataContext context)
    {
        return new(entity, context);
    }

    public static HardChallengeView Create(HardChallengeEntry? entity, MetadataHardChallengeSchedule meta, HardChallengeMetadataContext context)
    {
        return entity is not null ? new(entity, context) : new(meta, context);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(ScheduleId):
                OnPropertyChanged(nameof(Schedule));
                break;
            case nameof(ScheduleName):
                OnPropertyChanged(nameof(Schedule));
                break;
        }
    }
}
