// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public sealed partial class MaterialNativeResources : ResourceDictionary
{
    private static readonly EmptyObjectToVisibilityConverter HeaderVisibilityConverter = new();

    public MaterialNativeResources()
    {
        InitializeComponent();
    }

    // A ControlTemplate x:Bind never receives its converter lookup root, so a
    // {StaticResource} converter throws in the generated LookupConverter. Convert
    // through the same converter instance instead, keeping its empty-value rules.
    public static Visibility CreateHeaderVisibility(object? content)
    {
        return (Visibility)HeaderVisibilityConverter.Convert(content!, typeof(Visibility), null!, null!);
    }
}
