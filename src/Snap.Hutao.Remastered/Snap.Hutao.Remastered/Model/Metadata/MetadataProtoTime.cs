// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Globalization;

namespace Snap.Hutao.Remastered.Model.Metadata;

/// <summary>
/// ISO-8601 conversion used by the protobuf-net timestamp members. protobuf-net has no scalar
/// surrogate for <see cref="DateTimeOffset"/>, so timestamp members keep the public value and expose
/// a string member with the same field number.
/// </summary>
public static class MetadataProtoTime
{
    public static string ToWire(DateTimeOffset value)
    {
        return value.ToString("o", CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset FromWire(string value)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset result)
            ? result
            : default;
    }
}
