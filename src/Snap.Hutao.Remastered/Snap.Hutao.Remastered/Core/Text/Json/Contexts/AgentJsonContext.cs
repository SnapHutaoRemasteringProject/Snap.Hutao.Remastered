// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.AI.Migration;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(LegacyModelProfile))]
[JsonSerializable(typeof(LegacyConversation))]
internal sealed partial class AgentJsonContext : JsonSerializerContext;
