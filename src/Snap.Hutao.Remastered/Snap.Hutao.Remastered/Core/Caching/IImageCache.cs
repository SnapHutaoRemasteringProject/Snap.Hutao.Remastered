// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.IO;
using System.IO;

namespace Snap.Hutao.Remastered.Core.Caching;

public interface IImageCache
{
    ValueFile GetFileFromCategoryAndName(string category, string fileName);

    ValueTask<ValueFile> GetFileFromCacheAsync(Uri uri);

    ValueTask<ValueFile> GetFileFromCacheAsync(Uri uri, ElementTheme theme);

    ValueTask<CachedImageResource> GetImageAsync(Uri uri, ElementTheme theme, CachedImageResource? failedResource = null);

    void Remove(Uri uriForCachedItem);

    ValueTask StoreFileAsync(string category, string fileName, Stream source, CancellationToken token = default);

    ValueTask<(int Files, long Bytes)> RemoveDuplicateFilesAsync(CancellationToken token = default);
}
