// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Wiki;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class WikiAvatarPage : ScopedPage, INotifyPropertyChanged
{
    public WikiAvatarPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(ViewModel)));
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<WikiAvatarViewModel>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WikiAvatarViewModel ViewModel => DataContext;

    public new WikiAvatarViewModel DataContext { get => Unsafe.As<WikiAvatarViewModel>(base.DataContext); set => base.DataContext = value; }
}
