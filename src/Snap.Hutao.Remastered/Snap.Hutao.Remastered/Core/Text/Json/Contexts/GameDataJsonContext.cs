// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Service.Game.PathAbstraction;
using Snap.Hutao.Remastered.Web.Enka.Model;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(uint[]))]
[JsonSerializable(typeof(EnkaResponse))]
[JsonSerializable(typeof(ImmutableArray<GamePathEntry>))]
[JsonSerializable(typeof(ImmutableArray<AspectRatio>))]
internal sealed partial class GameDataJsonContext : JsonSerializerContext;
