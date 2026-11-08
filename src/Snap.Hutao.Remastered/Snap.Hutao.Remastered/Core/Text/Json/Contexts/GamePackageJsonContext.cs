// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.Package;
using Snap.Hutao.Remastered.Service.Game.Package.Advanced;
using Snap.Hutao.Remastered.Web.Hoyolab.Downloader;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(VersionItem))]
[JsonSerializable(typeof(PredownloadStatus))]
[JsonSerializable(typeof(Response<SophonBuild>))]
internal sealed partial class GamePackageJsonContext : JsonSerializerContext;
