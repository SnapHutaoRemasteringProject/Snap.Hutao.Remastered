// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.Text.Json;

// Root file contracts used by MetadataService, including scattered element files.
// Every MetadataFileStrategy read through MetadataService.FromCacheOrFileAsync<T> must have a
// root here. scripts/Test-HttpJsonContracts.ps1 verifies the compiled assembly, so run it after
// building when a metadata strategy or model is added.
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Achievement.Achievement>), TypeInfoPropertyName = "MetadataFile001")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Achievement.AchievementGoal>), TypeInfoPropertyName = "MetadataFile002")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Avatar.Avatar>), TypeInfoPropertyName = "MetadataFile003")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Avatar.ProfilePicture>), TypeInfoPropertyName = "MetadataFile004")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Chapter>), TypeInfoPropertyName = "MetadataFile005")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Combine>), TypeInfoPropertyName = "MetadataFile006")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.CookRecipe>), TypeInfoPropertyName = "MetadataFile007")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.GachaEvent>), TypeInfoPropertyName = "MetadataFile008")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.GrowCurve>), TypeInfoPropertyName = "MetadataFile009")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.HardChallengeSchedule>), TypeInfoPropertyName = "MetadataFile010")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.HyperLinkName>), TypeInfoPropertyName = "MetadataFile011")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Item.BeyondItem>), TypeInfoPropertyName = "MetadataFile012")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Item.DisplayItem>), TypeInfoPropertyName = "MetadataFile013")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Item.Material>), TypeInfoPropertyName = "MetadataFile014")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.MainQuest>), TypeInfoPropertyName = "MetadataFile015")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Monster.Monster>), TypeInfoPropertyName = "MetadataFile016")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Promote>), TypeInfoPropertyName = "MetadataFile017")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Reliquary.Reliquary>), TypeInfoPropertyName = "MetadataFile018")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Reliquary.ReliquaryMainAffix>), TypeInfoPropertyName = "MetadataFile019")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Reliquary.ReliquaryMainAffixLevel>), TypeInfoPropertyName = "MetadataFile020")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Reliquary.ReliquarySet>), TypeInfoPropertyName = "MetadataFile021")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Reliquary.ReliquarySubAffix>), TypeInfoPropertyName = "MetadataFile022")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.RoleCombatSchedule>), TypeInfoPropertyName = "MetadataFile023")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Tower.TowerFloor>), TypeInfoPropertyName = "MetadataFile024")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Tower.TowerLevel>), TypeInfoPropertyName = "MetadataFile025")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Tower.TowerSchedule>), TypeInfoPropertyName = "MetadataFile026")]
[JsonSerializable(typeof(global::System.Collections.Immutable.ImmutableArray<global::Snap.Hutao.Remastered.Model.Metadata.Weapon.Weapon>), TypeInfoPropertyName = "MetadataFile027")]
internal sealed partial class MetadataFileJsonContext : JsonSerializerContext;
