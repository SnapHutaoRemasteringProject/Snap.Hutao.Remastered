// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.AI.Models;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AgentConversation))]
internal sealed partial class AgentConversationJsonContext : JsonSerializerContext;
