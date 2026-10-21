// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableTextReasoningContent : ObservableAIContent
{
    [ObservableProperty]
    [JsonIgnore]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    public static ObservableTextReasoningContent Create(TextReasoningContent textReasoningContent)
    {
        return new()
        {
            Text = textReasoningContent.Text,
        };
    }

    public void Update(ObservableTextReasoningContent textReasoningContent)
    {
        Text += textReasoningContent.Text;
    }
}
