// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

public sealed class LockQueryProcess
{
    [JsonPropertyName("processId")]
    public uint ProcessId { get; set; }

    [JsonPropertyName("applicationName")]
    public string ApplicationName { get; set; } = default!;

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = default!;

    [JsonPropertyName("openHandleCount")]
    public ulong OpenHandleCount { get; set; }

    [JsonPropertyName("sectionHandleCount")]
    public ulong SectionHandleCount { get; set; }

    [JsonPropertyName("mappedViewCount")]
    public ulong MappedViewCount { get; set; }

    [JsonPropertyName("closedOpenHandleCount")]
    public ulong ClosedOpenHandleCount { get; set; }

    [JsonPropertyName("closedSectionHandleCount")]
    public ulong ClosedSectionHandleCount { get; set; }

    [JsonPropertyName("unmappedViewCount")]
    public ulong UnmappedViewCount { get; set; }
}
