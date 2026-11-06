// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;

namespace Snap.Hutao.Remastered.Core.Caching;

public interface IImageCacheDownloadOperation
{
    /// <summary>
    /// 把 <paramref name="uri"/> 下载到 <paramref name="baseFile"/>。
    /// </summary>
    /// <param name="uri">源地址。</param>
    /// <param name="baseFile">目标文件。</param>
    /// <param name="token">
    /// 取消标记：会传进 HTTP 请求并中止响应体复制，因此取消后不会再有后台写入；
    /// 调用方负责给出超时（共享客户端的 HttpClient 是无限超时）。
    /// </param>
    ValueTask DownloadFileAsync(Uri uri, ValueFile baseFile, CancellationToken token = default);
}
