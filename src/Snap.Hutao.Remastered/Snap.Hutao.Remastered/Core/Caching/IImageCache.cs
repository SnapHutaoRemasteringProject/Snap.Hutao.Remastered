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

    /// <summary>
    /// 只查找已存在的图片文件，不发起任何下载：先看可写缓存，再看安装包内自带的资源包。
    /// 供需要「有就用、没有就算了」的调用方使用，避免 <see cref="GetFileFromCacheAsync(Uri)"/>
    /// 在缓存未命中时自行发起一次无法取消、也没有超时的下载。
    /// </summary>
    /// <returns>已存在的本地文件；两者都没有时返回 <see langword="null"/>。</returns>
    ValueFile? TryGetExistingFile(Uri uri);

    ValueTask<ValueFile> GetFileFromCacheAsync(Uri uri, ElementTheme theme);

    ValueTask<CachedImageResource> GetImageAsync(Uri uri, ElementTheme theme, CachedImageResource? failedResource = null);

    void Remove(Uri uriForCachedItem);

    ValueTask StoreFileAsync(string category, string fileName, Stream source, CancellationToken token = default);

    ValueTask<(int Files, long Bytes)> RemoveDuplicateFilesAsync(CancellationToken token = default);
}
