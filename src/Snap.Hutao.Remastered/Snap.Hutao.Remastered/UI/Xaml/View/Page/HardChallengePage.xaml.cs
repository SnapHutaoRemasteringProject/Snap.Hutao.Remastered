// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.HardChallenge;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class HardChallengePage : ScopedPage
{
    public HardChallengePage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<HardChallengeViewModel>();
    }

    public new HardChallengeViewModel DataContext { get => Unsafe.As<HardChallengeViewModel>(base.DataContext); set => base.DataContext = value; }
}
