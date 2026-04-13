// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

namespace Snap.Hutao.Remastered.ViewModel.Home;

internal sealed partial class ActivityTemplateSelector : DataTemplateSelector
{
    private static readonly DataTemplate EmptyTemplate = new();

    public DataTemplate? SignInTemplate { get; set; }

    public DataTemplate? DoubleTemplate { get; set; }

    public DataTemplate? ExploreTemplate { get; set; }

    public DataTemplate? LiBenTemplate { get; set; }

    public DataTemplate? TowerTemplate { get; set; }

    public DataTemplate? RoleCombatTemplate { get; set; }

    public DataTemplate? HardChallengeTemplate { get; set; }

    public DataTemplate? OtherTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is Act act)
        {
            return act switch
            {
                ActSignIn signIn => SignInTemplate,
                ActDouble @double => DoubleTemplate,
                ActExplore explore => ExploreTemplate,
                ActLiBen liBen => LiBenTemplate,
                ActTower tower => TowerTemplate,
                ActRoleCombat roleCombat => RoleCombatTemplate,
                ActHardChallenge hardChallenge => HardChallengeTemplate,
                ActOther other => OtherTemplate,
                _ => EmptyTemplate,
            };
        }

        return base.SelectTemplateCore(item, container);
    }
}
