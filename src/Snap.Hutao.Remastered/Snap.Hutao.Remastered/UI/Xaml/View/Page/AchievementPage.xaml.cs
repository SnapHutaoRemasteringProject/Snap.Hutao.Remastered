// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Achievement;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class AchievementPage : ScopedPage, INotifyPropertyChanged
{
    public AchievementPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(ViewModel)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AchievementViewModel ViewModel => DataContext;

    public new AchievementViewModel DataContext { get => Unsafe.As<AchievementViewModel>(base.DataContext); set => base.DataContext = value; }

    protected override void LoadingOverride()
    {
        InitializeDataContext<AchievementViewModel>();
    }
}
