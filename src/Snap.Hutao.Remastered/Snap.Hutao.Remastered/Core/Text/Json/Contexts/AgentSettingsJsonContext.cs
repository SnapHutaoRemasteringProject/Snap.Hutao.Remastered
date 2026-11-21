// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Settings;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AgentSettings))]
[JsonSerializable(typeof(ModelProviderType))]
[JsonSerializable(typeof(ReasoningEffort))]
internal sealed partial class AgentSettingsJsonContext : JsonSerializerContext;
