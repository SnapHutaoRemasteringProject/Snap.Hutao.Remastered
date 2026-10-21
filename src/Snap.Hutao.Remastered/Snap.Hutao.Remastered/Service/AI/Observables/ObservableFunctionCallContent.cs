// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableFunctionCallContent : ObservableToolCallContent
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string Name { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayArguments))]
    public partial string? Arguments { get; set; }

    [JsonIgnore]
    public override string DisplayName { get => Name; }

    [JsonIgnore]
    public override string? DisplayArguments { get => Arguments; }

    public static ObservableFunctionCallContent Create(FunctionCallContent functionCallContent, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            CallId = functionCallContent.CallId,
            Name = functionCallContent.Name,
            Arguments = SerializeArguments(functionCallContent.Arguments, jsonOptions),
        };
    }

    public void Update(ObservableFunctionCallContent functionCallContent)
    {
        Name = functionCallContent.Name;
        Arguments = functionCallContent.Arguments;
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
