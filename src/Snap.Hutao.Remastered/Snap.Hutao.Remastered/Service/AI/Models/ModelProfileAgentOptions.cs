// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Core.AI;

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed partial class ModelProfileAgentOptions : ObservableObject, ICopyFrom<ModelProfileAgentOptions>
{
    [ObservableProperty]
    public partial float? Temperature { get; set; }

    [ObservableProperty]
    public partial float? TopP { get; set; }

    [ObservableProperty]
    public partial EnumBox<ReasoningEffort>? ReasoningEffort { get; set; } = EnumBox.Of(Microsoft.Extensions.AI.ReasoningEffort.High);

    [ObservableProperty]
    public partial bool ThinkingEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool OmitReasoningEffortWhenThinkingDisabled { get; set; } = true;

    [ObservableProperty]
    public partial int? MaxInputTokens { get; set; }

    [ObservableProperty]
    public partial int? MaxContextWindowTokens { get; set; }

    [ObservableProperty]
    public partial int? MaxOutputTokens { get; set; }
}
