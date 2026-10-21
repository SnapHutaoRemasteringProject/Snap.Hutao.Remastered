// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.Core.AI;

internal ref struct BooleanTrueScope : IDisposable
{
    private ref bool storage;

    public BooleanTrueScope(ref bool storage)
    {
        this.storage = ref storage;
        storage = true;
    }

    public static BooleanTrueScope Create(ref bool value)
    {
        return new(ref value);
    }

    public void Dispose()
    {
        storage = false;
    }
}
