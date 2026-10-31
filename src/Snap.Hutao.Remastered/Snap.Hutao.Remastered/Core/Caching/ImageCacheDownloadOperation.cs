// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.ViewModel.Guide;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;

namespace Snap.Hutao.Remastered.Core.Caching;

[Service(ServiceLifetime.Singleton, typeof(IImageCacheDownloadOperation))]
[HttpClient(HttpClientConfiguration.Default)]
[PrimaryHttpMessageHandler(MaxConnectionsPerServer = 8)]
public sealed partial class ImageCacheDownloadOperation : IImageCacheDownloadOperation
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly IHttpClientFactory httpClientFactory;

    [GeneratedConstructor]
    public partial ImageCacheDownloadOperation(IServiceProvider serviceProvider);

    public async ValueTask DownloadFileAsync(Uri uri, ValueFile baseFile)
    {
        using (HttpClient httpClient = httpClientFactory.CreateClient(nameof(ImageCacheDownloadOperation)))
        {
            try
            {
                await DownloadFileUsingHttpClientAsync(httpClient, uri, baseFile).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                // Ignore
            }
        }

        if (!File.Exists(baseFile))
        {
            throw InternalImageCacheException.Throw($"'{uri.OriginalString}': File not exists after download attempt.");
        }
    }

    private async ValueTask DownloadFileUsingHttpClientAsync(HttpClient httpClient, Uri uri, ValueFile baseFile)
    {
        HttpRequestMessageBuilder requestMessageBuilder = httpRequestMessageBuilderFactory
            .Create()
            .SetRequestUri(uri)
            .SetStaticResourceControlHeadersIfRequired()
            .Get();

        using (HttpRequestMessage requestMessage = requestMessageBuilder.HttpRequestMessage)
        {
            using (HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw responseMessage.StatusCode switch
                    {
                        HttpStatusCode.NotFound => InternalImageCacheException.Throw($"Unable to download file from '{uri.OriginalString}': 404 Not Found"),
                        _ => InternalImageCacheException.Throw($"Unexpected HTTP status code {responseMessage.StatusCode}"),
                    };
                }

                if (responseMessage.Content.Headers.ContentType?.MediaType is MediaTypeNames.Application.Json)
                {
                    InternalImageCacheException.Throw($"Unexpected content type: {MediaTypeNames.Application.Json}");
                }

                using (Stream httpStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    string? directoryName = Path.GetDirectoryName(baseFile);
                    ArgumentException.ThrowIfNullOrEmpty(directoryName);

                    try
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                    catch (DirectoryNotFoundException dnfEx)
                    {
                        throw InternalImageCacheException.Throw($"Unable to create folder at '{directoryName}'", dnfEx);
                    }

                    FileStream fileStream;
                    try
                    {
                        fileStream = File.Create(baseFile);
                    }
                    catch (IOException ex)
                    {
                        // The process cannot access the file '?' because it is being used by another process.
                        throw InternalImageCacheException.Throw($"Unable to create file at '{baseFile}'", ex);
                    }

                    try
                    {
                        using (fileStream)
                        {
                            await httpStream.CopyToAsync(fileStream).ConfigureAwait(false);
                        }
                    }
                    catch (IOException ex)
                    {
                        // Received an unexpected EOF or 0 bytes from the transport stream.
                        // Unable to read data from the transport connection: 杩滅▼涓绘満寮鸿揩鍏抽棴浜嗕竴涓幇鏈夌殑杩炴帴銆? SocketException: ConnectionReset
                        // Unable to read data from the transport connection: 浣犵殑涓绘満涓殑杞欢涓浜嗕竴涓凡寤虹珛鐨勮繛鎺ャ€? SocketException: ConnectionAborted
                        // HttpIOException: The response ended prematurely. (ResponseEnded)
                        // 纾佺洏绌洪棿涓嶈冻銆?: '?'.
                        throw InternalImageCacheException.Throw("Unable to copy stream content to file", ex);
                    }
                }
            }
        }
    }
}

