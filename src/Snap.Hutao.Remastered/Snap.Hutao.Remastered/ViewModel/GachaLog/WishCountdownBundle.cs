// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public sealed class WishCountdownBundle
{
    public required ImmutableArray<Countdown> OrangeAvatars { get; init; }

    public required ImmutableArray<Countdown> PurpleAvatars { get; init; }

    public required ImmutableArray<Countdown> OrangeWeapons { get; init; }

    public required ImmutableArray<Countdown> PurpleWeapons { get; init; }

    [JsonIgnore]
    public List<Countdown> OrangeAvatarItems => OrangeAvatars.IsDefaultOrEmpty ? [] : new(OrangeAvatars);

    [JsonIgnore]
    public List<Countdown> PurpleAvatarItems => PurpleAvatars.IsDefaultOrEmpty ? [] : new(PurpleAvatars);

    [JsonIgnore]
    public List<Countdown> OrangeWeaponItems => OrangeWeapons.IsDefaultOrEmpty ? [] : new(OrangeWeapons);

    [JsonIgnore]
    public List<Countdown> PurpleWeaponItems => PurpleWeapons.IsDefaultOrEmpty ? [] : new(PurpleWeapons);
}
