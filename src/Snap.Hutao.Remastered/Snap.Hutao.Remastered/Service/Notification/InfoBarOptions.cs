// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.Service.Notification;

public sealed partial class InfoBarOptions : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTextSelectionEnabled))]
    public partial InfoBarSeverity Severity { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial object? Content { get; set; }

    [ObservableProperty]
    public partial string? ActionButtonContent { get; set; }

    [ObservableProperty]
    public partial ICommand? ActionButtonCommand { get; set; }

    [ObservableProperty]
    public partial int MilliSecondsDelay { get; set; }

    public bool IsTextSelectionEnabled { get => Severity >= InfoBarSeverity.Warning; }

    public static InfoBarOptions Create(InfoBarMessage message)
    {
        return new()
        {
            Severity = message.Severity,
            Title = message.Title,
            Message = message.Message,
            Content = message.Content,
            ActionButtonContent = message.ActionButtonContent,
            ActionButtonCommand = message.ActionButtonCommand,
            MilliSecondsDelay = message.MilliSecondsDelay,
        };
    }
}
