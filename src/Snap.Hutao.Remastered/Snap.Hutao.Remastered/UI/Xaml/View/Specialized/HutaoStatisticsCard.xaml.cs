// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.GachaLog;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Specialized;

/// <summary>
/// 胡桃云祈愿统计卡片
/// </summary>
public sealed partial class HutaoStatisticsCard : UserControl
{
    public HutaoStatisticsCard()
    {
        InitializeComponent();
    }

    public new HutaoWishSummary DataContext { get => Unsafe.As<HutaoWishSummary>(base.DataContext); set => base.DataContext = value; }
}
