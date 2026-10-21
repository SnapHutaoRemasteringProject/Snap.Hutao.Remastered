// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.ChatElements;

[DependencyProperty<object>("PendingValue")]
[DependencyProperty<object>("ApprovedValue")]
[DependencyProperty<object>("RejectedValue")]
internal sealed partial class ChatToolApprovalResultToObjectConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value switch
        {
            true => ApprovedValue,
            false => RejectedValue,
            _ => PendingValue,
        }) ?? DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
