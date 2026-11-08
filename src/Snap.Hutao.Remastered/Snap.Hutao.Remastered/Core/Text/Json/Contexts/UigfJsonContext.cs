// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.InterChange.GachaLog;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(UIGF3))]
[JsonSerializable(typeof(UIGF4))]
[JsonSerializable(typeof(UIGF42))]
internal sealed partial class UigfJsonContext : JsonSerializerContext;
