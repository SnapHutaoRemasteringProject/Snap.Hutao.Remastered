// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

public sealed class DebugPrivilegeState
{
    [JsonPropertyName("attempted")]
    public bool Attempted { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = default!;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("autoElevationAttempted")]
    public bool AutoElevationAttempted { get; set; }

    [JsonPropertyName("assignedToToken")]
    public bool AssignedToToken { get; set; }

    [JsonPropertyName("previouslyEnabled")]
    public bool PreviouslyEnabled { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("requiresElevation")]
    public bool RequiresElevation { get; set; }

    [JsonPropertyName("errorCode")]
    public uint ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = default!;
}
