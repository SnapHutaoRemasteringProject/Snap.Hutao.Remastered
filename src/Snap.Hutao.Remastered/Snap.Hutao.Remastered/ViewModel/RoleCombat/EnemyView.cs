// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

internal sealed class EnemyView : INameIcon<Uri>
{
    private EnemyView(RoleCombatEnemy roleCombatEnemy)
    {
        Name = roleCombatEnemy.Name;
        Icon = roleCombatEnemy.Icon.ToUri();
        Level = LevelFormat.Format(roleCombatEnemy.Level);
    }

    public string Name { get; }

    public Uri Icon { get; }

    public string Level { get; }

    public static EnemyView Create(RoleCombatEnemy roleCombatEnemy)
    {
        return new(roleCombatEnemy);
    }
}