// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.IO;

/// <summary>
/// Thrown by <see cref="StallGuardStream"/> when the wrapped stream delivers no data within the
/// configured read timeout.
/// </summary>
public sealed class StallDetectedException : Exception
{
    public StallDetectedException(TimeSpan timeout)
        : base($"The stream delivered no data within {timeout}.")
    {
    }
}
