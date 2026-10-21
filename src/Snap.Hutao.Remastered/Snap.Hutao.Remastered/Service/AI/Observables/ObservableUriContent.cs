// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableUriContent : ObservableAIContent
{
    [ObservableProperty]
    public partial Uri Uri { get; set; }

    [ObservableProperty]
    public partial string MediaType { get; set; }
}
