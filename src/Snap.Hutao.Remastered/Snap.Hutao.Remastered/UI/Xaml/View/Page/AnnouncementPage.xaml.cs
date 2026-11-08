// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Home;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class AnnouncementPage : ScopedPage
{
    public AnnouncementPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<AnnouncementViewModel>();
    }

    public new AnnouncementViewModel DataContext { get => Unsafe.As<AnnouncementViewModel>(base.DataContext); set => base.DataContext = value; }
}
