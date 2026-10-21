// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System;
using Microsoft.Extensions.AI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

internal sealed class ChatRoleToVisibilityConverter : IValueConverter
{
    public string? Role { get; set; }

    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ChatRole chatRole || string.IsNullOrEmpty(Role))
        {
            return Visibility.Collapsed;
        }

        ChatRole role = new(Role);
        bool isVisible = chatRole == role;
        if (IsInverted)
        {
            isVisible = !isVisible;
        }

        return isVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
