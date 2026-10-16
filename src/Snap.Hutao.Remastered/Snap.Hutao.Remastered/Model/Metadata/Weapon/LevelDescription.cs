// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

public sealed partial class LevelDescription : ObservableObject
{
    public required int Level { get; init => SetProperty(ref field, value); }

    [JsonIgnore]
    public string FormattedLevel { get => SH.FormatModelWeaponAffix(Level + 1); }

    public required string Description { get; init => SetProperty(ref field, value); }
}
