// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;
using System.IO;

namespace Snap.Hutao.Remastered.Core.Caching;

public readonly record struct CachedImageResource(ValueFile File, DateTime LastWriteTimeUtc, long Length)
{
    internal static CachedImageResource? TryRead(string path)
    {
        FileInfo file = new(path);
        return file.Exists && file.Length > 0 ? new(path, file.LastWriteTimeUtc, file.Length) : null;
    }
}
