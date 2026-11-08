// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.ViewModel.Abstraction;
using Snap.Hutao.Remastered.ViewModel.DailyNote;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Card;

public sealed partial class DailyNoteCard : MaterialCardButton
{
    public DailyNoteCard(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.InitializeViewModelSlim<ViewModel.DailyNote.DailyNoteViewModelSlim>(serviceProvider);
    }

    public new DailyNoteViewModelSlim DataContext { get => Unsafe.As<DailyNoteViewModelSlim>(base.DataContext); set => base.DataContext = value; }
}
