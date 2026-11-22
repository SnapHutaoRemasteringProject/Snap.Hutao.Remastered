// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoAvatarCurve = Snap.Hutao.Remastered.Metadata.AvatarCurve;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class AvatarCurveAdapter : ProtoCategoryAdapter<ProtoAvatarCurve, GrowCurve>
{
    public AvatarCurveAdapter()
        : base(MetadataCategories.AvatarCurve)
    {
    }

    public override IEnumerable<ProtoAvatarCurve> Enumerate(LanguageMetadata language)
    {
        return language.AvatarCurves;
    }

    public override GrowCurve Map(ProtoAvatarCurve message, MetadataConvert convert)
    {
        return new()
        {
            Level = (Level)message.Level,
            Curves = convert.GrowCurveCollection(message.Curves, nameof(message.Curves)),
        };
    }
}
