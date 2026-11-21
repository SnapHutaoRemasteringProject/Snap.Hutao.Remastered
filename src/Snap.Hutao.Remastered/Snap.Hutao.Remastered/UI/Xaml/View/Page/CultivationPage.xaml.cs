// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Cultivation;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class CultivationPage : ScopedPage, INotifyPropertyChanged
{
    public CultivationPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(ViewModel)));
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<CultivationViewModel>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultivationViewModel ViewModel => DataContext;

    public new CultivationViewModel DataContext { get => Unsafe.As<CultivationViewModel>(base.DataContext); set => base.DataContext = value; }
}
