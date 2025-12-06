// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.HardChallenge;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

internal sealed partial class HardChallengePage : ScopedPage
{
    public HardChallengePage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<HardChallengeViewModel>();
    }
}