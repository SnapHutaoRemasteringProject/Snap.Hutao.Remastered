// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoMainQuest = Snap.Hutao.Remastered.Metadata.MainQuest;
using ProtoMainQuestTag = Snap.Hutao.Remastered.Metadata.MainQuestTag;
using ProtoQuestShowType = Snap.Hutao.Remastered.Metadata.QuestShowType;
using ProtoQuestType = Snap.Hutao.Remastered.Metadata.QuestType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class MainQuestAdapter : ProtoCategoryAdapter<ProtoMainQuest, MainQuest>
{
    public MainQuestAdapter()
        : base(MetadataCategories.MainQuest)
    {
    }

    public override IEnumerable<ProtoMainQuest> Enumerate(LanguageMetadata language)
    {
        return language.MainQuests;
    }

    public override MainQuest Map(ProtoMainQuest message, MetadataConvert convert)
    {
        return new()
        {
            Id = (MainQuestId)message.Id,
            Type = convert.ToEnum<ProtoQuestType, QuestType>(message.Type, nameof(message.Type)),
            Title = convert.Text(message.Title),
            Description = convert.Text(message.Description),
            UnlockDescription = convert.OptionalText(message.UnlockDescription),
            ChapterId = (ChapterId)message.ChapterId,
            SortWeight = message.SortWeight,
            RecommendLevel = message.RecommendLevel,
            ActivityId = message.ActivityId,
            MainQuestTag = convert.ToEnum<ProtoMainQuestTag, MainQuestTag>(message.MainQuestTag, nameof(message.MainQuestTag)),
            ShowType = convert.ToEnum<ProtoQuestShowType, QuestShowType>(message.ShowType, nameof(message.ShowType)),
            Repeatable = message.Repeatable,
            Series = message.Series,
            TaskId = message.TaskId,
            RewardList = [.. message.RewardList.Select(reward => new IdCount { Id = (MaterialId)reward.Id, Count = reward.Count })],
        };
    }
}
