// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;

namespace Snap.Hutao.Remastered.Service.Abstraction.Property;

public sealed partial class StructUsingJsonDbProperty<T> : StructUsingCustomDbProperty<T>
    where T : struct
{
    public StructUsingJsonDbProperty(IServiceProvider serviceProvider, string key, Func<T> defaultValueFactory)
        : base(serviceProvider, key, defaultValueFactory, From, To)
    {
    }

    public StructUsingJsonDbProperty(IServiceProvider serviceProvider, string key, T defaultValue)
        : this(serviceProvider, key, () => defaultValue)
    {
    }

    private static T From(string source)
    {
        return JsonSerialize.Deserialize<T>(source, JsonOptions.Default);
    }

    private static string To(T value)
    {
        return JsonSerialize.Serialize(value, JsonOptions.Default);
    }
}