// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using Snap.Hutao.Remastered.Web.Hutao.HutaoAsAService;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Web.Hutao;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(HutaoResponse<ImmutableArray<GitRepository>>), TypeInfoPropertyName = "GitRepositoriesResponse")]
[JsonSerializable(typeof(PassportTokenExchange.Request), TypeInfoPropertyName = "PassportExchangeRequest")]
[JsonSerializable(typeof(Web.Response.Response<PassportTokenExchange.Result>), TypeInfoPropertyName = "PassportExchangeResponse")]
[JsonSerializable(typeof(Web.Response.Response<JsonElement>), TypeInfoPropertyName = "JsonElementResponse")]
[JsonSerializable(typeof(UserGameRole), TypeInfoPropertyName = "BindingUserGameRole")]
[JsonSerializable(typeof(Dictionary<string, string>), TypeInfoPropertyName = "StringDictionary")]
[JsonSerializable(typeof(HutaoResponse<ImmutableArray<Announcement>>), TypeInfoPropertyName = "AnnouncementListResponse")]
internal sealed partial class InfrastructureJsonContext : JsonSerializerContext
{
    public static InfrastructureJsonContext Wire { get; } = new(JsonWireOptions.Create());
}
