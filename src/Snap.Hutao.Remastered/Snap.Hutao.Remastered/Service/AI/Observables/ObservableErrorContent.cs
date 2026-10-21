// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableErrorContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial string? ErrorCode { get; set; }

    [ObservableProperty]
    public partial string? Details { get; set; }

    public static ObservableErrorContent Create(string message, string? errorCode = default, string? details = default)
    {
        return new()
        {
            Message = message,
            ErrorCode = errorCode,
            Details = details,
        };
    }
}
