// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao.RoleCombat;
using Snap.Hutao.Remastered.Web.Hutao.SpiralAbyss;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(RoleCombatStatisticsItem))]
[JsonSerializable(typeof(Overview))]
[JsonSerializable(typeof(IReadOnlyList<AvatarAppearanceRank>))]
[JsonSerializable(typeof(IReadOnlyList<AvatarUsageRank>))]
[JsonSerializable(typeof(IReadOnlyList<AvatarConstellationInfo>))]
[JsonSerializable(typeof(IReadOnlyList<AvatarCollocation>))]
[JsonSerializable(typeof(IReadOnlyList<WeaponCollocation>))]
[JsonSerializable(typeof(IReadOnlyList<TeamAppearance>))]
internal sealed partial class CacheJsonContext : JsonSerializerContext;
