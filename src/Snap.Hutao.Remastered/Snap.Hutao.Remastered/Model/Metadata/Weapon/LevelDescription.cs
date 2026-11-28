// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

[ProtoContract]
public sealed partial class LevelDescription : ObservableObject
{
    [ProtoMember(1)]
    public int Level { get; init => SetProperty(ref field, value); }

    [JsonIgnore]
    public string FormattedLevel { get => SH.FormatModelWeaponAffix(Level + 1); }

    [ProtoMember(2)]
    public string Description { get; init => SetProperty(ref field, value); }
}
