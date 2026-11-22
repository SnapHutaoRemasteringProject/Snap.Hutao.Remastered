// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoAchievementGoal = Snap.Hutao.Remastered.Metadata.AchievementGoal;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class AchievementGoalAdapter : ProtoCategoryAdapter<ProtoAchievementGoal, Model.Metadata.Achievement.AchievementGoal>
{
    public AchievementGoalAdapter()
        : base(MetadataCategories.AchievementGoal)
    {
    }

    public override IEnumerable<ProtoAchievementGoal> Enumerate(LanguageMetadata language)
    {
        return language.AchievementGoals;
    }

    public override Model.Metadata.Achievement.AchievementGoal Map(ProtoAchievementGoal message, MetadataConvert convert)
    {
        return new()
        {
            Id = (AchievementGoalId)message.Id,
            Order = message.Order,
            Name = convert.Text(message.Name),
            FinishReward = message.FinishReward is { } reward
                ? new()
                {
                    Id = (MaterialId)reward.Id,
                    Count = reward.Count,
                }
                : null,
            Icon = convert.Text(message.Icon),
        };
    }
}
