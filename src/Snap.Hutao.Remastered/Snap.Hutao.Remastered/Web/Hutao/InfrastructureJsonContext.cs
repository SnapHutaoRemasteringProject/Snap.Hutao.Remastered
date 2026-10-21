// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Web.Hutao;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(HutaoResponse<ImmutableArray<GitRepository>>), TypeInfoPropertyName = "GitRepositoriesResponse")]
internal sealed partial class InfrastructureJsonContext : JsonSerializerContext
{
    public static InfrastructureJsonContext Wire { get; } = new(JsonWireOptions.Create());
}
