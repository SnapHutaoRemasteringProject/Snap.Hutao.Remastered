// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel;
using Snap.Hutao.Remastered.Web.Hutao.Redeem;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class TestPage : ScopedPage
{
    public TestPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<TestViewModel>();
    }

    public new TestViewModel DataContext { get => Unsafe.As<TestViewModel>(base.DataContext); set => base.DataContext = value; }

    public RedeemCodeTargetServiceType[] RedeemCodeTargetServiceTypes { get; } = Enum.GetValues<RedeemCodeTargetServiceType>();

    public InfoBarSeverity[] InfoBarSeverities { get; } = Enum.GetValues<InfoBarSeverity>();
}
