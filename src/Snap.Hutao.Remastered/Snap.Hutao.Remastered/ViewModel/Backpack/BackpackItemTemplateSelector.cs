// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public sealed partial class BackpackItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? WeaponTemplate { get; set; }

    public DataTemplate? ReliquaryTemplate { get; set; }

    public DataTemplate? DefaultTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            BackpackWeaponItemView => WeaponTemplate,
            BackpackReliquaryItemView => ReliquaryTemplate,
            _ => DefaultTemplate,
        };
    }
}
