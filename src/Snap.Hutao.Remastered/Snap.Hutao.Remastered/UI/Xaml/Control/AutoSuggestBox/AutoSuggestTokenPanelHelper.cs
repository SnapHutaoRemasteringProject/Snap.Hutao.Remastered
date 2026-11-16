// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;

// ItemsPresenter is the items panel's templated parent. Carry the owner's spacing
// on it so the panel can use native TemplateBinding instead of ancestor lookup.
[SuppressMessage("", "SH001", Justification = "Public attached-property owner referenced by XAML.")]
[DependencyProperty<double>("TokenSpacing", DefaultValue = 0D, IsAttached = true, TargetType = typeof(ItemsPresenter), NotNull = true)]
public static partial class AutoSuggestTokenPanelHelper
{
}
