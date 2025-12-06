// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.Wiki;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

internal static class WeaponTypeValueCollectionExtension
{
    extension(WeaponTypeValueCollection collection)
    {
        public ImmutableArray<PropertyCurveValue> ToPropertyCurveValues()
        {
            return collection.Array.SelectAsArray(static curve => new PropertyCurveValue(curve.Type, curve.Value, curve.InitValue));
        }
    }
}