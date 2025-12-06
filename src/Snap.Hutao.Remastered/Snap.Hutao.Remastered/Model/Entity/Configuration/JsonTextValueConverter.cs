// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Snap.Hutao.Remastered.Core.Text.Json;

namespace Snap.Hutao.Remastered.Model.Entity.Configuration;

internal sealed class JsonTextValueConverter<TPropertyType> : ValueConverter<TPropertyType, string>
{
    [SuppressMessage("", "SH007")]
    public JsonTextValueConverter()
        : base(
            static obj => JsonSerializer.Serialize(obj, JsonOptions.Default),
            static str => string.IsNullOrEmpty(str) ? default! : JsonSerializer.Deserialize<TPropertyType>(str, JsonOptions.Default)!)
    {
    }
}