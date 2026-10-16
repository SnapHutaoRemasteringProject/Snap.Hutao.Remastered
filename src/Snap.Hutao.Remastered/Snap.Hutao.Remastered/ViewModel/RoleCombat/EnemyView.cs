// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

public sealed partial class EnemyView : ObservableObject, INameIcon<Uri>
{
    private EnemyView(RoleCombatEnemy roleCombatEnemy)
    {
        Name = roleCombatEnemy.Name;
        Icon = roleCombatEnemy.Icon.ToUri();
        Level = LevelFormat.Format(roleCombatEnemy.Level);
    }

    public string Name { get; private set => SetProperty(ref field, value); }

    public Uri Icon { get; private set => SetProperty(ref field, value); }

    public string Level { get; private set => SetProperty(ref field, value); }

    public static EnemyView Create(RoleCombatEnemy roleCombatEnemy)
    {
        return new(roleCombatEnemy);
    }
}
