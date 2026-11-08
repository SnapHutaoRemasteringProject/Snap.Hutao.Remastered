// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Bridge;
using Snap.Hutao.Remastered.Web.Bridge.Model;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(JsParam))]
[JsonSerializable(typeof(JsParam<SharePayload>))]
[JsonSerializable(typeof(JsResult<Dictionary<string, string>>))]
[JsonSerializable(typeof(CaptureScreenshotResult))]
internal sealed partial class BridgeJsonContext : JsonSerializerContext;
