// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.WinUI.Converters;
using Microsoft.UI.Xaml;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.AI;

internal sealed class EmptyObjectToVisibilityConverter : EmptyObjectToObjectConverter
{
    private bool isInverted;

    public EmptyObjectToVisibilityConverter()
    {
        UpdateValues();
    }

    public bool IsInverted
    {
        get => isInverted;
        set
        {
            isInverted = value;
            UpdateValues();
        }
    }

    private void UpdateValues()
    {
        NotEmptyValue = isInverted
            ? Visibility.Collapsed
            : Visibility.Visible;
        EmptyValue = isInverted
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
