// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

public sealed class FileUnlockerErrorResponse
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = default!;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = default!;

    [JsonPropertyName("errorCode")]
    public uint ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = default!;
}
