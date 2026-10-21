// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableMcpServerToolCallContent : ObservableToolCallContent
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string? ServerName { get; set; }

    [ObservableProperty]
    public partial string? Arguments { get; set; }

    public static ObservableMcpServerToolCallContent Create(McpServerToolCallContent toolCallContent, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            CallId = toolCallContent.CallId,
            Name = toolCallContent.Name,
            ServerName = toolCallContent.ServerName,
            Arguments = SerializeArguments(toolCallContent.Arguments, jsonOptions),
        };
    }

    private static string? SerializeArguments(IDictionary<string, object?>? value, JsonSerializerOptions jsonOptions)
    {
        if (value is null or { Count: 0 })
        {
            return null;
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
