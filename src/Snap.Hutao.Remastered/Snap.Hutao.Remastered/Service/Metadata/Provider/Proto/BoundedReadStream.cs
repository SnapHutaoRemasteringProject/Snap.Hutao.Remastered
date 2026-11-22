// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.IO;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Read-only pass-through stream that refuses to hand out more than <c>limit</c> bytes. The bundle is
/// AES-CBC encrypted without an authentication tag, so the decompressed size must be bounded
/// explicitly instead of trusting the payload.
/// </summary>
internal sealed class BoundedReadStream : Stream
{
    private readonly Stream inner;
    private readonly long limit;

    private long totalRead;

    public BoundedReadStream(Stream inner, long limit)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        this.inner = inner;
        this.limit = limit;
    }

    public override bool CanRead
    {
        get => true;
    }

    public override bool CanSeek
    {
        get => false;
    }

    public override bool CanWrite
    {
        get => false;
    }

    public override long Length
    {
        get => throw new NotSupportedException();
    }

    public override long Position
    {
        get => totalRead;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        if (buffer.Length is 0)
        {
            return 0;
        }

        if (totalRead >= limit)
        {
            throw new InvalidDataException($"Metadata bundle exceeded the decompressed limit of {limit} bytes.");
        }

        int allowed = (int)Math.Min(buffer.Length, limit - totalRead);
        int read = inner.Read(buffer[..allowed]);
        totalRead += read;
        return read;
    }

    public override int ReadByte()
    {
        Span<byte> single = stackalloc byte[1];
        return Read(single) is 0 ? -1 : single[0];
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        // The caller owns the inner stream (it is part of a using chain); only stop using it.
        base.Dispose(disposing);
    }
}
