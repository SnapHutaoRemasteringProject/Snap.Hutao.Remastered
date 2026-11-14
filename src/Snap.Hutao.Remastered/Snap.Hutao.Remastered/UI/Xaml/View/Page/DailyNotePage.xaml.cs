// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.DailyNote;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class DailyNotePage : ScopedPage, INotifyPropertyChanged
{
    public DailyNotePage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(ViewModel)));
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<DailyNoteViewModel>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DailyNoteViewModel ViewModel => DataContext;

    public new DailyNoteViewModel DataContext { get => Unsafe.As<DailyNoteViewModel>(base.DataContext); set => base.DataContext = value; }
}
