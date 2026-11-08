// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.FileUnlocker;
using Snap.Hutao.Remastered.Service.Game.Account;
using Snap.Hutao.Remastered.Service.Geetest;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Core.Text.Json;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Risk))]
[JsonSerializable(typeof(RiskVerify))]
[JsonSerializable(typeof(LockUnlockResponse))]
[JsonSerializable(typeof(LockQueryData))]
[JsonSerializable(typeof(GeetestWebResponse))]
[JsonSerializable(typeof(AigisSession))]
[JsonSerializable(typeof(GeetestVerification))]
[JsonSerializable(typeof(DataWrapper<ImmutableArray<AccountInformation>>))]
internal sealed partial class UserJsonContext : JsonSerializerContext;
