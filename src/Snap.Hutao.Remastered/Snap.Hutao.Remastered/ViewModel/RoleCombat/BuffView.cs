// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

public sealed partial class BuffView : ObservableObject
{
    private BuffView(RoleCombatBuff roleCombatBuff)
    {
        Icon = roleCombatBuff.Icon.ToUri();
        Name = roleCombatBuff.Name;
        Description = roleCombatBuff.Description.Replace("\\n", "\n", StringComparison.OrdinalIgnoreCase);
    }

    public Uri Icon { get; private set => SetProperty(ref field, value); }

    public string Name { get; private set => SetProperty(ref field, value); }

    public string Description { get; private set => SetProperty(ref field, value); }

    public static BuffView Create(RoleCombatBuff roleCombatBuff)
    {
        return new(roleCombatBuff);
    }
}
