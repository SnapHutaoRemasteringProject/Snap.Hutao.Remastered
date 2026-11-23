// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.AI;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

internal sealed partial class AgentSettingsView : UserControl
{
    public AgentSettingsView() { InitializeComponent(); }

    public new AgentViewModel DataContext { get => Unsafe.As<AgentViewModel>(base.DataContext); set => base.DataContext = value; }
}
