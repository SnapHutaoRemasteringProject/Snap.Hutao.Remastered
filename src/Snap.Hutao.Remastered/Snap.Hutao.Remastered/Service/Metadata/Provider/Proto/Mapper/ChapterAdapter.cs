// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoChapter = Snap.Hutao.Remastered.Metadata.Chapter;
using ProtoQuestType = Snap.Hutao.Remastered.Metadata.QuestType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class ChapterAdapter : ProtoCategoryAdapter<ProtoChapter, Chapter>
{
    public ChapterAdapter()
        : base(MetadataCategories.Chapter)
    {
    }

    public override IEnumerable<ProtoChapter> Enumerate(LanguageMetadata language)
    {
        return language.Chapters;
    }

    public override Chapter Map(ProtoChapter message, MetadataConvert convert)
    {
        return new()
        {
            Id = (ChapterId)message.Id,
            GroupId = (ChapterGroupId)message.GroupId,
            BeginQuestId = (QuestId)message.BeginQuestId,
            EndQuestId = (QuestId)message.EndQuestId,
            NeedPlayerLevel = message.NeedPlayerLevel,
            Number = convert.OptionalText(message.Number),
            Title = convert.OptionalText(message.Title),
            Icon = convert.Text(message.Icon),
            ChapterIcon = convert.Text(message.ChapterIcon),
            ImageTitle = convert.OptionalText(message.ImageTitle),
            SerialNumberIcon = convert.Text(message.SerialNumberIcon),

            // proto carries the city as a bare int32; keep the raw number (the JSON source does too).
            CityId = convert.RawEnum<City>(message.CityId, nameof(message.CityId)),
            QuestType = convert.ToEnum<ProtoQuestType, QuestType>(message.QuestType, nameof(message.QuestType)),
        };
    }
}
