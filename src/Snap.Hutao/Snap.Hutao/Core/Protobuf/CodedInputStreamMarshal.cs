// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Core.Protobuf;

internal static class CodedInputStreamMarshal
{
    extension(CodedInputStream stream)
    {
        public bool TryPeekTag(out uint tag)
        {
            tag = stream.PeekTag();
            return tag is not 0;
        }

        public bool TryReadTag(out uint tag)
        {
            tag = stream.ReadTag();
            return tag is not 0;
        }

        public CodedInputStream UnsafeReadLengthDelimitedStream()
        {
            return new(ReadRawBytes(stream, stream.ReadLength()));
        }
    }

    // internal byte[] ReadRawBytes(int size)
    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    private static extern byte[] ReadRawBytes(CodedInputStream stream, int size);
}