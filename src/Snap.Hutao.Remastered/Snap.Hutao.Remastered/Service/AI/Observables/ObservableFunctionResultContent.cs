// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableFunctionResultContent : ObservableToolResultContent
{
    [ObservableProperty]
    public partial string? Result { get; set; }

    public static ObservableFunctionResultContent Create(FunctionResultContent functionResultContent, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            CallId = functionResultContent.CallId,
            Result = SerializeResult(functionResultContent.Result, jsonOptions),
        };
    }

    public void Update(ObservableFunctionResultContent functionResultContent)
    {
        Result = functionResultContent.Result;
    }

    private static string? SerializeResult(object? value, JsonSerializerOptions jsonOptions)
    {
        if (value is JsonElement { ValueKind: JsonValueKind.String } stringElement)
        {
            return stringElement.GetString();
        }

        try
        {
            return JsonSerializer.Serialize(value, jsonOptions);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("AI tool content serialization failed: {0}", exception);
            return value?.ToString();
        }
    }
}
