// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.Overlay;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View;

public sealed partial class OverlayView : UserControl
{
    public OverlayView()
    {
        InitializeComponent();
    }

    public new OverlayViewModel DataContext { get => Unsafe.As<OverlayViewModel>(base.DataContext); set => base.DataContext = value; }
}
