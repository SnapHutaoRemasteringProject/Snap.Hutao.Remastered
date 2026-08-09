// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

public sealed class LockQueryData
{
    [JsonPropertyName("inUse")]
    public bool InUse { get; set; }

    [JsonPropertyName("rebootRequired")]
    public bool RebootRequired { get; set; }

    [JsonPropertyName("errorCode")]
    public uint ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = default!;

    [JsonPropertyName("debugPrivilege")]
    public DebugPrivilegeState DebugPrivilege { get; set; } = default!;

    [JsonPropertyName("processes")]
    public List<LockQueryProcess> Processes { get; set; } = [];
}
