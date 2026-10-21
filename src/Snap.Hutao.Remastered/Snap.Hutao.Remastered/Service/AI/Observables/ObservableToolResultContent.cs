// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

[JsonPolymorphic]
[JsonDerivedType(typeof(ObservableCodeInterpreterToolResultContent), "code_interpreter_tool_result")]
[JsonDerivedType(typeof(ObservableFunctionResultContent), "function_result")]
[JsonDerivedType(typeof(ObservableImageGenerationToolResultContent), "image_generation_tool_result")]
[JsonDerivedType(typeof(ObservableMcpServerToolResultContent), "mcp_server_tool_result")]
[JsonDerivedType(typeof(ObservableWebSearchToolResultContent), "web_search_tool_result")]
internal partial class ObservableToolResultContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string CallId { get; set; }
}
