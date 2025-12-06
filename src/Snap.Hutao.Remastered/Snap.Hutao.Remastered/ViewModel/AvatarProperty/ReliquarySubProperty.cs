// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

internal class ReliquarySubProperty
{
    public ReliquarySubProperty(FightProperty type, string value)
    {
        Name = type.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture);
        Value = value;
    }

    public string? Name { get; }

    public string Value { get; }
}