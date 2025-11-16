// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.IO;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.Yae;

internal static class BinaryReaderExtension
{
    extension(BinaryReader reader)
    {
        public unsafe T Read<T>()
            where T : unmanaged
        {
            T data = default;
            reader.ReadExactly(new(&data, sizeof(T)));
            return data;
        }
    }
}