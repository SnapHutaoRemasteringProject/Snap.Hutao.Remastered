// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

[JsonPolymorphic]
[JsonDerivedType(typeof(ObservableCodeInterpreterToolCallContent), "code_interpreter_tool_call")]
[JsonDerivedType(typeof(ObservableFunctionCallContent), "function_call")]
[JsonDerivedType(typeof(ObservableImageGenerationToolCallContent), "image_generation_tool_call")]
[JsonDerivedType(typeof(ObservableMcpServerToolCallContent), "mcp_server_tool_call")]
[JsonDerivedType(typeof(ObservableWebSearchToolCallContent), "web_search_tool_call")]
internal partial class ObservableToolCallContent : ObservableAIContent
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string CallId { get; set; }

    [JsonIgnore]
    public virtual string DisplayName { get => CallId; }

    [JsonIgnore]
    public virtual string? DisplayArguments { get => null; }

    public static ObservableToolCallContent Create(ToolCallContent toolCallContent, JsonSerializerOptions jsonOptions)
    {
        return toolCallContent switch
        {
            CodeInterpreterToolCallContent codeInterpreterToolCallContent => ObservableCodeInterpreterToolCallContent.Create(codeInterpreterToolCallContent, jsonOptions),
            FunctionCallContent functionCallContent => ObservableFunctionCallContent.Create(functionCallContent, jsonOptions),
            ImageGenerationToolCallContent imageGenerationToolCallContent => ObservableImageGenerationToolCallContent.Create(imageGenerationToolCallContent),
            McpServerToolCallContent mcpServerToolCallContent => ObservableMcpServerToolCallContent.Create(mcpServerToolCallContent, jsonOptions),
            WebSearchToolCallContent webSearchToolCallContent => ObservableWebSearchToolCallContent.Create(webSearchToolCallContent),
            _ => new()
            {
                CallId = toolCallContent.CallId,
            },
        };
    }
}
