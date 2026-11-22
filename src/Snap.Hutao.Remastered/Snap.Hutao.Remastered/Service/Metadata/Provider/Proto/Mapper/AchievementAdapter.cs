// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Achievement;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoAchievement = Snap.Hutao.Remastered.Metadata.Achievement;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class AchievementAdapter : ProtoCategoryAdapter<ProtoAchievement, Model.Metadata.Achievement.Achievement>
{
    public AchievementAdapter()
        : base(MetadataCategories.Achievement)
    {
    }

    public override IEnumerable<ProtoAchievement> Enumerate(LanguageMetadata language)
    {
        return language.Achievements;
    }

    public override Model.Metadata.Achievement.Achievement Map(ProtoAchievement message, MetadataConvert convert)
    {
        Reward finishReward = message.FinishReward is { } reward
            ? new()
            {
                Id = (MaterialId)reward.Id,
                Count = reward.Count,
            }
            : throw HutaoException.InvalidOperation("Achievement entry has no finish reward");

        return new()
        {
            Id = (AchievementId)message.Id,
            Goal = (AchievementGoalId)message.Goal,
            Order = message.Order,
            PreviousId = (AchievementId)message.PreviousId,
            Title = convert.Text(message.Title),
            Description = convert.Text(message.Description),
            FinishReward = finishReward,
            IsDeleteWatcherAfterFinish = message.IsDeleteWatcherAfterFinish,
            Progress = message.Progress,
            Icon = convert.OptionalText(message.Icon),
            Version = convert.Text(message.Version),
            IsDailyQuest = message.IsDailyQuest,
        };
    }
}
