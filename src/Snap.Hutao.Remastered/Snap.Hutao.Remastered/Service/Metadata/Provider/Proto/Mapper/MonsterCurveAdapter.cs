// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoMonsterCurve = Snap.Hutao.Remastered.Metadata.MonsterCurve;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class MonsterCurveAdapter : ProtoCategoryAdapter<ProtoMonsterCurve, GrowCurve>
{
    public MonsterCurveAdapter()
        : base(MetadataCategories.MonsterCurve)
    {
    }

    public override IEnumerable<ProtoMonsterCurve> Enumerate(LanguageMetadata language)
    {
        return language.MonsterCurves;
    }

    public override GrowCurve Map(ProtoMonsterCurve message, MetadataConvert convert)
    {
        return new()
        {
            Level = (Level)message.Level,
            Curves = convert.GrowCurveCollection(message.Curves, nameof(message.Curves)),
        };
    }
}
