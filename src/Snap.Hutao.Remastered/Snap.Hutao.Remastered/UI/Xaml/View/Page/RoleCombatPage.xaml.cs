// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.RoleCombat;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class RoleCombatPage : ScopedPage
{
    public RoleCombatPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<RoleCombatViewModel>();
    }

    public new RoleCombatViewModel DataContext { get => Unsafe.As<RoleCombatViewModel>(base.DataContext); set => base.DataContext = value; }
}
