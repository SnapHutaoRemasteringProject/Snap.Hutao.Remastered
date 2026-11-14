using Snap.Hutao.Remastered.Core.Graphics.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed unsafe class MemoryBufferByteAccessTest
{
    [TestMethod]
    public void NativeSlotReturnsWritablePixelMemoryWithByteCapacity()
    {
        using BufferFixture fixture = new(12);
        Span<uint> pixels = MemoryBufferByteAccess.GetBuffer<uint>(fixture.Instance);
        Assert.AreEqual(3, pixels.Length);
        pixels[1] = 0xAABBCCDD;
        Assert.AreEqual(0xAABBCCDDu, ((uint*)fixture.State->Buffer)[1]);
    }

    [TestMethod]
    public void FailingHresultIsThrownBeforeReadingTheBuffer()
    {
        using BufferFixture fixture = new(12);
        fixture.State->Result = unchecked((int)0x80004005);
        Assert.ThrowsExactly<COMException>(() => MemoryBufferByteAccess.GetBuffer<byte>(fixture.Instance));
    }

    [TestMethod]
    public void EmptyNullBufferProducesEmptySpan()
    {
        using BufferFixture fixture = new(0);
        fixture.State->Buffer = null;
        Assert.IsTrue(MemoryBufferByteAccess.GetBuffer<byte>(fixture.Instance).IsEmpty);
    }

    [TestMethod]
    public void MalformedBufferCannotExposeNullMemoryOrOverflowedLength()
    {
        using BufferFixture fixture = new(uint.MaxValue);
        Assert.ThrowsExactly<OverflowException>(() => MemoryBufferByteAccess.GetBuffer<byte>(fixture.Instance));
        fixture.State->Buffer = null;
        Assert.ThrowsExactly<InvalidOperationException>(() => MemoryBufferByteAccess.GetBuffer<byte>(fixture.Instance));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetBuffer(nint instance, byte** data, uint* capacity)
    {
        BufferState* state = (BufferState*)instance;
        *data = state->Buffer;
        *capacity = state->Capacity;
        return state->Result;
    }

    private struct BufferState
    {
        public nint Vtable;
        public byte* Buffer;
        public uint Capacity;
        public int Result;
    }

    private sealed class BufferFixture : IDisposable
    {
        private readonly nint* vtable = (nint*)NativeMemory.AllocZeroed(4, (nuint)sizeof(nint));
        private readonly byte* allocation = (byte*)NativeMemory.AllocZeroed(16);
        public BufferState* State { get; } = (BufferState*)NativeMemory.AllocZeroed((nuint)sizeof(BufferState));
        public nint Instance => (nint)State;

        public BufferFixture(uint capacity)
        {
            vtable[3] = (nint)(delegate* unmanaged[Stdcall]<nint, byte**, uint*, int>)&GetBuffer;
            State->Vtable = (nint)vtable;
            State->Buffer = allocation;
            State->Capacity = capacity;
        }

        public void Dispose()
        {
            NativeMemory.Free(State);
            NativeMemory.Free(allocation);
            NativeMemory.Free(vtable);
        }
    }
}
