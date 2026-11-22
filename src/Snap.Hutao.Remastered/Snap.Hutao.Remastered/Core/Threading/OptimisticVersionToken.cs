// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.Threading;

/// <summary>
/// A version snapshot produced by <see cref="OptimisticVersion.Capture"/> or
/// <see cref="OptimisticVersion.Advance"/>. The default value is never current.
/// </summary>
public readonly struct OptimisticVersionToken
{
    private readonly OptimisticVersion? owner;
    private readonly int version;

    internal OptimisticVersionToken(OptimisticVersion owner, int version)
    {
        this.owner = owner;
        this.version = version;
    }

    /// <summary>
    /// Gets a value indicating whether the captured state has not been replaced since
    /// this token was created.
    /// </summary>
    public bool IsCurrent
    {
        get => owner is not null && owner.IsCurrent(version);
    }
}
