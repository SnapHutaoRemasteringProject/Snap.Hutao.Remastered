// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json.Annotation;

namespace Snap.Hutao.Remastered.Core.Text.Json.Converter;

public sealed class NumberStringEnumConverter<TEnum>() : UnsafeEnumConverter<TEnum>(JsonEnumHandling.NumberString, JsonEnumHandling.NumberString)
    where TEnum : struct, Enum;
