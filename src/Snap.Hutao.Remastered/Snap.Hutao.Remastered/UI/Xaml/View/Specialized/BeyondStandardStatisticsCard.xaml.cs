// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.GachaLog;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Specialized;

[DependencyProperty<bool>("ShowUpPull", DefaultValue = true, NotNull = true)]
[DependencyProperty<bool>("ShowCombinedTotal", DefaultValue = false, NotNull = true)]
[DependencyProperty<bool>("ShowCombinedModeToggle", DefaultValue = true, NotNull = true)]
public sealed partial class BeyondStatisticsCard : UserControl
{
    private TypedWishSummary? Summary => DataContext as TypedWishSummary;

    public BeyondStatisticsCard()
    {
        InitializeComponent();
        CombinedModeToggle.Click += (sender, args) => ShowCombinedTotal = !ShowCombinedTotal;
        RegisterPropertyChangedCallback(ShowCombinedTotalProperty, OnShowCombinedTotalChanged);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is TypedWishSummary summary)
        {
            summary.ShowCombinedTotal = ShowCombinedTotal;
        }

        Bindings.Update();
    }

    private static void OnShowCombinedTotalChanged(DependencyObject sender, DependencyProperty dp)
    {
        BeyondStatisticsCard card = Unsafe.As<BeyondStatisticsCard>(sender);
        card.CombinedModeToggle.IsChecked = card.ShowCombinedTotal;
        if (card.DataContext is TypedWishSummary summary)
        {
            summary.ShowCombinedTotal = card.ShowCombinedTotal;
        }
    }
}
