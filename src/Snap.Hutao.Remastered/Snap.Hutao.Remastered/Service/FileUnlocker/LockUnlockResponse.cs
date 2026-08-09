// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

public sealed class LockUnlockResponse
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = default!;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("force")]
    public bool Force { get; set; }

    [JsonPropertyName("unlockErrorCode")]
    public uint UnlockErrorCode { get; set; }

    [JsonPropertyName("unlockErrorMessage")]
    public string UnlockErrorMessage { get; set; } = default!;

    [JsonPropertyName("forcedTerminationAttempted")]
    public bool ForcedTerminationAttempted { get; set; }

    [JsonPropertyName("closedOpenHandleCount")]
    public ulong ClosedOpenHandleCount { get; set; }

    [JsonPropertyName("closedSectionHandleCount")]
    public ulong ClosedSectionHandleCount { get; set; }

    [JsonPropertyName("unmappedViewCount")]
    public ulong UnmappedViewCount { get; set; }

    [JsonPropertyName("unloadedModuleCount")]
    public ulong UnloadedModuleCount { get; set; }

    [JsonPropertyName("terminatedProcessCount")]
    public ulong TerminatedProcessCount { get; set; }

    [JsonPropertyName("before")]
    public LockQueryData Before { get; set; } = default!;

    [JsonPropertyName("after")]
    public LockQueryData After { get; set; } = default!;

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
