// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using ProtoGachaEvent = Snap.Hutao.Remastered.Metadata.GachaEvent;
using ProtoGachaType = Snap.Hutao.Remastered.Metadata.GachaType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class GachaEventAdapter : ProtoCategoryAdapter<ProtoGachaEvent, GachaEvent>
{
    public GachaEventAdapter()
        : base(MetadataCategories.GachaEvent)
    {
    }

    public override IEnumerable<ProtoGachaEvent> Enumerate(LanguageMetadata language)
    {
        return language.GachaEvents;
    }

    public override GachaEvent Map(ProtoGachaEvent message, MetadataConvert convert)
    {
        return new()
        {
            Name = convert.Text(message.Name),
            Version = convert.Text(message.Version),
            Order = message.Order,
            Banner = convert.RequiredUri(message.Banner, nameof(message.Banner)),
            Banner2 = convert.OptionalUri(message.Banner2, nameof(message.Banner2)),
            From = convert.Time(message.From, nameof(message.From)),
            To = convert.Time(message.To, nameof(message.To)),
            Type = convert.ToEnum<ProtoGachaType, GachaType>(message.Type, nameof(message.Type)),
            UpOrangeList = [.. message.UpOrangeList.Select(value => value)],
            UpPurpleList = [.. message.UpPurpleList.Select(value => value)],
        };
    }
}
