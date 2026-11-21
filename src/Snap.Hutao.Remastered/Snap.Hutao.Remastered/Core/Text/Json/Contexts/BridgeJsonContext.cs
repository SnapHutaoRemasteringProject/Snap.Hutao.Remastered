// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Bridge;
using Snap.Hutao.Remastered.Web.Bridge.Model;
using Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(JsParam))]
[JsonSerializable(typeof(JsParam<SharePayload>))]
[JsonSerializable(typeof(ActionTypePayload))]
[JsonSerializable(typeof(CookieTokenPayload))]
[JsonSerializable(typeof(PushPagePayload))]
[JsonSerializable(typeof(DataSignV2Payload))]
[JsonSerializable(typeof(JsResult<Dictionary<string, string>>))]
[JsonSerializable(typeof(JsResult<Dictionary<string, object>>))]
[JsonSerializable(typeof(UserGender))]
[JsonSerializable(typeof(CaptureScreenshotResult))]
internal sealed partial class BridgeJsonContext : JsonSerializerContext;
