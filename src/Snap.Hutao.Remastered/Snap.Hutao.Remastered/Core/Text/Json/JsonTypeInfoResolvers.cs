// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json.Annotation;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

public static class JsonTypeInfoResolvers
{
    private static readonly Type JsonEnumHandlingAttributeType = typeof(JsonEnumHandlingAttribute);

    [RequiresUnreferencedCode("This modifier reads reflection property attributes.")]
    [RequiresDynamicCode("This modifier constructs reflection enum converters.")]
    public static void ResolveEnumType(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind is not JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            if (!property.PropertyType.IsEnum)
            {
                continue;
            }

            if (property.AttributeProvider is not { } provider)
            {
                continue;
            }

            if (provider.GetCustomAttributes(JsonEnumHandlingAttributeType, false) is [JsonEnumHandlingAttribute attr])
            {
                property.CustomConverter = attr.CreateConverter(property);
            }
        }
    }
}
