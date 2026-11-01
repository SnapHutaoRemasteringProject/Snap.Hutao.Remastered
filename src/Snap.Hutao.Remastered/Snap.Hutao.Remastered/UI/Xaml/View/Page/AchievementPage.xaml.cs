// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Achievement;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class AchievementPage : ScopedPage
{
    public AchievementPage()
    {
        InitializeComponent();
    }

    public new AchievementViewModel DataContext { get => (AchievementViewModel)base.DataContext; set => base.DataContext = value; }

    protected override void LoadingOverride()
    {
        InitializeDataContext<AchievementViewModel>();
    }
}
