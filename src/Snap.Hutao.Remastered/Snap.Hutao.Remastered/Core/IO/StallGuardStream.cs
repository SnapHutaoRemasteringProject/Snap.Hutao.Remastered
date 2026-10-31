// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.IO;

namespace Snap.Hutao.Remastered.Core.IO;

/// <summary>
/// Read-only stream wrapper that fails a read when the wrapped stream delivers no data within
/// <c>readTimeout</c>.
/// </summary>
/// <remarks>
/// The guard owns the cancellation of the read it starts: on a timeout it cancels and awaits the
/// pending read before surfacing <see cref="StallDetectedException"/>. Awaiting the aborted read
/// is what keeps it from faulting as an unobserved task. Only asynchronous reads are supported.
/// </remarks>
public sealed class StallGuardStream : Stream
{
    private readonly Stream inner;
    private readonly TimeSpan readTimeout;

    public StallGuardStream(Stream inner, TimeSpan readTimeout)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(readTimeout, TimeSpan.Zero);

        this.inner = inner;
        this.readTimeout = readTimeout;
    }

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<int> read = inner.ReadAsync(buffer, readCts.Token).AsTask();

        try
        {
            return await read.WaitAsync(readTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (read.IsCompletedSuccessfully)
            {
                // The read and the timeout raced; the data has already arrived.
                return read.Result;
            }

            await AbortAsync(read, readCts).ConfigureAwait(false);
            throw new StallDetectedException(readTimeout);
        }
        catch (OperationCanceledException)
        {
            await ObserveAsync(read).ConfigureAwait(false);
            throw;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Stall detection requires asynchronous reads.");
    }

    public override void Flush()
    {
        throw new NotSupportedException();
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

    private static async ValueTask AbortAsync(Task<int> read, CancellationTokenSource readCts)
    {
        // The read is bound to this source. Cancelling it and awaiting the result before the
        // source is disposed keeps the aborted read from faulting as an unobserved task.
        await readCts.CancelAsync().ConfigureAwait(false);
        await ObserveAsync(read).ConfigureAwait(false);
    }

    private static async ValueTask ObserveAsync(Task<int> read)
    {
        try
        {
            await read.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
        {
            // The read was aborted on purpose; its failure is the expected outcome. Awaiting it
            // here is what keeps that failure from surfacing as an unobserved task exception.
        }
    }
}
