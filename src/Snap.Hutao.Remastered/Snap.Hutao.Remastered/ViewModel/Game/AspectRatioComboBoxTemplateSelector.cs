// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.ViewModel.Game;

public sealed partial class AspectRatioComboBoxTemplateSelector : DataTemplateSelector
{
    public DataTemplate ListTemplate { get; set; } = default!;

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.ContentPresenter))]
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return container is ContentPresenter ? default : ListTemplate;
    }
}