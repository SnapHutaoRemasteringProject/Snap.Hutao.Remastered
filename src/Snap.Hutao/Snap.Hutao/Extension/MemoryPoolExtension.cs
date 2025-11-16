// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Buffers;

namespace Snap.Hutao.Extension;

internal static partial class MemoryPoolExtension
{
    extension<T>(MemoryPool<T> memoryPool)
    {
        public IMemoryOwner<T> RentExactly(int bufferSize)
        {
            IMemoryOwner<T> memoryOwner = memoryPool.Rent(bufferSize);
            return new ExactSizedMemoryOwner<T>(memoryOwner, bufferSize);
        }
    }

    private sealed partial class ExactSizedMemoryOwner<T> : IMemoryOwner<T>
    {
        private readonly IMemoryOwner<T> owner;
        private readonly int bufferSize;

        public ExactSizedMemoryOwner(IMemoryOwner<T> owner, int bufferSize)
        {
            this.owner = owner;
            this.bufferSize = bufferSize;
        }

        public Memory<T> Memory { get => owner.Memory[..bufferSize]; }

        public void Dispose()
        {
            owner.Dispose();
        }
    }
}