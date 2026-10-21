// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal partial class ObservableInputRequestContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string RequestId { get; set; }
}
