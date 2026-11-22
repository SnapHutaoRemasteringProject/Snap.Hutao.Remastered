// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.Threading;

/// <summary>
/// Monotonic version of state that can be replaced while asynchronous work is running.
/// Work captures a token before it starts and discards its result once that token is no
/// longer current, so a late response cannot overwrite the state that replaced it.
/// This is not a lock: it never blocks, and a token check is not atomic with the write
/// that follows it — it only rejects results whose source was replaced.
/// </summary>
public sealed class OptimisticVersion
{
    private int version;

    /// <summary>
    /// Returns a token for the current version.
    /// </summary>
    public OptimisticVersionToken Capture()
    {
        return new(this, Volatile.Read(ref version));
    }

    /// <summary>
    /// Records a replacement and returns the token of the new version.
    /// Tokens captured before this call stop being current.
    /// </summary>
    public OptimisticVersionToken Advance()
    {
        return new(this, Interlocked.Increment(ref version));
    }

    internal bool IsCurrent(int expectedVersion)
    {
        return Volatile.Read(ref version) == expectedVersion;
    }
}
