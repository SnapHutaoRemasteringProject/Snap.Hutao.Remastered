// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Buffers;
using System.IO;
using System.Text;

namespace Snap.Hutao.Remastered.Service.GachaLog.QueryProvider;

public sealed partial class GachaLogQueryWebCacheProvider
{
    internal static string? Match(MemoryStream stream, bool isOversea)
    {
        using (IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.RentExactly((int)stream.Length))
        {
            Span<byte> span = memoryOwner.Memory.Span;
            stream.ReadExactly(span);

            ReadOnlySpan<byte> match = isOversea
                ? "https://gs.hoyoverse.com/genshin/event/e20190909gacha"u8
                : "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha"u8;

            int index = span.LastIndexOf(match);
            if (index >= 0)
            {
                ReadOnlySpan<byte> target = span[(index + match.Length)..];
                int terminator = target.IndexOf((byte)0);
                return terminator < 0 ? null : Encoding.UTF8.GetString(target[..terminator]);
            }

            return null;
        }
    }
}
