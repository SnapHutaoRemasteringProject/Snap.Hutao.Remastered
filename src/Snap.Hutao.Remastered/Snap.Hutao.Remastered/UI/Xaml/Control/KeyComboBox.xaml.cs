// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.UI.Input;
using Snap.Hutao.Remastered.Win32.UI.Input.KeyboardAndMouse;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

[DependencyProperty<NameValue<VIRTUAL_KEY>>("SelectedItem", CreateDefaultValueCallbackName = nameof(CreateSelectedItemDefaultValue))]
[DependencyProperty<object>("Header")]
public sealed partial class KeyComboBox : UserControl
{
    public KeyComboBox()
    {
        InitializeComponent();
    }

    public ImmutableArray<NameValue<VIRTUAL_KEY>> Keys { get; } = VirtualKeys.Values;

    private static object CreateSelectedItemDefaultValue()
    {
        return VirtualKeys.Values[^1];
    }
}
