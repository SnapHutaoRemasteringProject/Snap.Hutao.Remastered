// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;
using System.IO;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

/// <summary>
/// Regression coverage for the static resource download stall guard. The reported failure was an
/// unobserved task exception whose inner exception was
/// "The CancellationTokenSource has been disposed."; it came from work that outlived the source
/// that owned it, so these tests pin the lifetime contract of the read the guard starts.
/// </summary>
[TestClass]
public sealed class StallGuardStreamTest
{
    [TestMethod]
    public async Task SuccessfulReadIsForwarded()
    {
        using MemoryStream inner = new([1, 2, 3]);
        using StallGuardStream guard = new(inner, TimeSpan.FromSeconds(5));

        byte[] buffer = new byte[8];
        int bytesRead = await guard.ReadAsync(buffer);

        Assert.AreEqual(3, bytesRead);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, buffer[..3]);
    }

    [TestMethod]
    public async Task StallIsReportedAndTheAbortedReadIsObserved()
    {
        using BlockingStream inner = new();
        using UnobservedSourceFailures probe = new();
        using StallGuardStream guard = new(inner, TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsExactlyAsync<StallDetectedException>(() => guard.ReadAsync(new byte[16]).AsTask());

        probe.CollectAndAssertNone();
    }

    [TestMethod]
    public async Task CallerCancellationPropagatesAndTheAbortedReadIsObserved()
    {
        using CancellationTokenSource caller = new();
        using BlockingStream inner = new();
        using UnobservedSourceFailures probe = new();
        using StallGuardStream guard = new(inner, TimeSpan.FromSeconds(30));

        Task<int> read = guard.ReadAsync(new byte[16], caller.Token).AsTask();
        caller.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => read);
        probe.CollectAndAssertNone();
    }

    private abstract class ReadOnlyTestStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class BlockingStream : ReadOnlyTestStream
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 0;
        }
    }

    /// <summary>
    /// Captures the exact failure this class guards against: an unobserved task whose inner
    /// exception is the disposed <see cref="CancellationTokenSource"/> error.
    /// </summary>
    private sealed class UnobservedSourceFailures : IDisposable
    {
        private readonly List<UnobservedTaskExceptionEventArgs> failures = [];

        public UnobservedSourceFailures()
        {
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        public void Dispose()
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }

        public void CollectAndAssertNone()
        {
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.AreEqual(0, failures.Count);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception is AggregateException aggregate
                && aggregate.Flatten().InnerExceptions.Any(
                    static exception => exception is ObjectDisposedException
                        && exception.Message.Contains("The CancellationTokenSource has been disposed.", StringComparison.Ordinal)))
            {
                failures.Add(e);
            }

            // Mark observed so the test run itself never trips the process-wide handler.
            e.SetObserved();
        }
    }
}
