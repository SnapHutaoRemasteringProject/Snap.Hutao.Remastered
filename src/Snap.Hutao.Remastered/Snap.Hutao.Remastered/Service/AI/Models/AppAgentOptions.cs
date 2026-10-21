// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.AI;

namespace Snap.Hutao.Remastered.Service.AI.Models;

// DO NOT use AgentOptionsNormalizer.Normalize* call in the setter, because the options
// from UI will be normalized eventually before using, and we don't want to normalize them twice.
internal sealed partial class AppAgentOptions : ObservableObject, ICopyFrom<AppAgentOptions>
{
    [ObservableProperty]
    public partial string UserName { get; set; } = AgentOptionsNormalizer.DefaultUserName;

    [ObservableProperty]
    public partial string? SystemPrompt { get; set; } = AgentOptionsNormalizer.DefaultSystemPrompt;

    [ObservableProperty]
    public partial int? MaximumIterationsPerRequest { get; set; } = AgentOptionsNormalizer.DefaultMaximumIterationsPerRequest;
}
