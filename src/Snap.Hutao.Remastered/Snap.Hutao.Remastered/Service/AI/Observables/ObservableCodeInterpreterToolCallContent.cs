// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableCodeInterpreterToolCallContent : ObservableToolCallContent
{
    [ObservableProperty]
    public partial ObservableAIContentCollection? Inputs { get; set; }

    public static ObservableCodeInterpreterToolCallContent Create(CodeInterpreterToolCallContent toolCallContent, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            CallId = toolCallContent.CallId,
            Inputs = ObservableAIContentCollection.Create(toolCallContent.Inputs, jsonOptions),
        };
    }
}
