// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoTowerFloor = Snap.Hutao.Remastered.Metadata.TowerFloor;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class TowerFloorAdapter : ProtoCategoryAdapter<ProtoTowerFloor, Model.Metadata.Tower.TowerFloor>
{
    public TowerFloorAdapter()
        : base(MetadataCategories.TowerFloor)
    {
    }

    public override IEnumerable<ProtoTowerFloor> Enumerate(LanguageMetadata language)
    {
        return language.TowerFloors;
    }

    public override Model.Metadata.Tower.TowerFloor Map(ProtoTowerFloor message, MetadataConvert convert)
    {
        return new()
        {
            Id = (TowerFloorId)message.Id,
            Index = message.Index,
            LevelGroupId = (TowerLevelGroupId)message.LevelGroupId,
            Background = convert.Text(message.Background),
            Descriptions = [.. message.Descriptions.Select(convert.Text)],
            FirstDescriptions = message.FirstDescriptions.Count is 0 ? default(ImmutableArray<string>) : [.. message.FirstDescriptions.Select(convert.Text)],
            SecondDescriptions = message.SecondDescriptions.Count is 0 ? default(ImmutableArray<string>) : [.. message.SecondDescriptions.Select(convert.Text)],
        };
    }
}
