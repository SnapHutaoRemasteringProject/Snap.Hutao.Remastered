// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using Snap.Hutao.Remastered.Core.Protobuf;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;

public sealed partial class YaeData : IDisposable
{
    private readonly IMemoryOwner<byte> owner;
    private readonly int contentLength;

    public YaeData(YaeCommandKind kind, IMemoryOwner<byte> owner, int contentLength, uint cmdId = 0)
    {
        Kind = kind;
        this.owner = owner;
        this.contentLength = contentLength;
        CmdId = cmdId;
    }

    ~YaeData()
    {
        Dispose();
    }

    public static YaeData SessionEnd { get => new(YaeCommandKind.SessionEnd, IMemoryOwner<byte>.Empty, 0); }

    public YaeCommandKind Kind { get; }

    /// <summary>
    /// 仅 <see cref="YaeCommandKind.ResponsePacket"/> 有效，表示该数据包对应的游戏命令 Id。
    /// </summary>
    public uint CmdId { get; }

    public ByteString Bytes { get => ByteStringMarshal.Create(owner.Memory[..contentLength]); }

    public ref readonly YaePropertyTypeValue PropertyTypeValue
    {
        get => ref MemoryMarshal.AsRef<YaePropertyTypeValue>(owner.Memory.Span[..contentLength]);
    }

    public void Dispose()
    {
        owner.Dispose();
        GC.SuppressFinalize(this);
    }
}