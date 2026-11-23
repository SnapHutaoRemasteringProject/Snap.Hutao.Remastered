// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.AI.Compatibility.OpenAIChatCompletion;
using Snap.Hutao.Remastered.Service.AI.Models;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AgentConversation))]
[JsonSerializable(typeof(RoundTripInMemoryChatHistoryProvider.State))]
internal sealed partial class AgentConversationJsonContext : JsonSerializerContext;
