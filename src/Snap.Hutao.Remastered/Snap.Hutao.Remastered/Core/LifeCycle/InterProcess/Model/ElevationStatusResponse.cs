// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;

public sealed class ElevationStatusResponse
{
    [JsonConstructor]
    public ElevationStatusResponse(bool isElevated, int processId)
    {
        IsElevated = isElevated;
        ProcessId = processId;
    }

    public bool IsElevated { get; }

    public int ProcessId { get; }
}
