// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableWebSearchToolResultContent : ObservableToolResultContent
{
    [ObservableProperty]
    public partial ObservableAIContentCollection? Outputs { get; set; }
}
