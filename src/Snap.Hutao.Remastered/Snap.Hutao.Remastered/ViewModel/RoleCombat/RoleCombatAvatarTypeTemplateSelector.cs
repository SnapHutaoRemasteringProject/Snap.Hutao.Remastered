// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

internal sealed partial class RoleCombatAvatarTypeTemplateSelector : DataTemplateSelector
{
    private static readonly DataTemplate EmptyDataTemplate = new();

    public DataTemplate? TrialTemplate { get; set; }

    public DataTemplate? SupportTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is RoleCombatAvatarType type)
        {
            return type switch
            {
                RoleCombatAvatarType.Trial => TrialTemplate,
                RoleCombatAvatarType.Support => SupportTemplate,
                _ => EmptyDataTemplate,
            };
        }

        return base.SelectTemplateCore(item, container);
    }
}