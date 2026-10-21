// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.ObjectPool;

namespace Snap.Hutao.Remastered.Core.AI.ObjectPool;

public static class ObjectPoolExtensions
{
    extension<T>(ObjectPool<T> objectPool)
        where T : class
    {
        public ObjectPoolLease<T> Rent()
        {
            return new(objectPool);
        }
    }
}
