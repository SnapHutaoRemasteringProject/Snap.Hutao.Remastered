// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoWeaponCurve = Snap.Hutao.Remastered.Metadata.WeaponCurve;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class WeaponCurveAdapter : ProtoCategoryAdapter<ProtoWeaponCurve, GrowCurve>
{
    public WeaponCurveAdapter()
        : base(MetadataCategories.WeaponCurve)
    {
    }

    public override IEnumerable<ProtoWeaponCurve> Enumerate(LanguageMetadata language)
    {
        return language.WeaponCurves;
    }

    public override GrowCurve Map(ProtoWeaponCurve message, MetadataConvert convert)
    {
        return new()
        {
            Level = (Level)message.Level,
            Curves = convert.GrowCurveCollection(message.Curves, nameof(message.Curves)),
        };
    }
}
