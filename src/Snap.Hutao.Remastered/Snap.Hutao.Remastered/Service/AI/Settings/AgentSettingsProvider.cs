// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.AI.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

[Service(ServiceLifetime.Singleton, typeof(ISettingsProvider<AgentSettings>))]
internal sealed class AgentSettingsProvider(IServiceProvider serviceProvider)
    : JsonSettingsProvider<AgentSettings>("agent-settings", serviceProvider.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey.Settings));
