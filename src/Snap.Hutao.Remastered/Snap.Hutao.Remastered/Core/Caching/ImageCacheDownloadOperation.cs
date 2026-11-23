// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
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

    public async ValueTask DownloadFileAsync(Uri uri, ValueFile baseFile, CancellationToken token = default)
    {
        using HttpClient httpClient = httpClientFactory.CreateClient(nameof(ImageCacheDownloadOperation));
        try
        {
            await DownloadFileUsingHttpClientAsync(httpClient, uri, baseFile, token).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw InternalImageCacheException.Throw($"Unable to download file from '{uri.OriginalString}'", ex);
        }
    }

    private async ValueTask DownloadFileUsingHttpClientAsync(HttpClient httpClient, Uri uri, ValueFile baseFile, CancellationToken token)
    {
        HttpRequestMessageBuilder requestMessageBuilder = httpRequestMessageBuilderFactory
            .Create()
            .SetRequestUri(uri)
            .SetStaticResourceControlHeadersIfRequired()
            .Get();

        using HttpRequestMessage requestMessage = requestMessageBuilder.HttpRequestMessage;
        using HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        if (!responseMessage.IsSuccessStatusCode)
        {
            throw responseMessage.StatusCode switch
            {
                HttpStatusCode.NotFound => InternalImageCacheException.Throw($"Unable to download file from '{uri.OriginalString}': 404 Not Found"),
                _ => InternalImageCacheException.Throw($"Unexpected HTTP status code {responseMessage.StatusCode}"),
            };
        }

        if (responseMessage.Content.Headers.ContentType?.MediaType is MediaTypeNames.Application.Json or MediaTypeNames.Text.Html)
        {
            throw InternalImageCacheException.Throw($"Unexpected content type: {responseMessage.Content.Headers.ContentType}");
        }

        using Stream httpStream = await responseMessage.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        string? directoryName = Path.GetDirectoryName(baseFile);
        ArgumentException.ThrowIfNullOrEmpty(directoryName);
        string temporary = baseFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(directoryName);
            using (FileStream fileStream = File.Create(temporary))
            {
                await httpStream.CopyToAsync(fileStream, token).ConfigureAwait(false);
                if (fileStream.Length == 0)
                {
                    throw InternalImageCacheException.Throw($"'{uri.OriginalString}': Empty download.");
                }
            }

            token.ThrowIfCancellationRequested();
            File.Move(temporary, baseFile, true);
        }
        catch (IOException ex)
        {
            throw InternalImageCacheException.Throw($"Unable to save file at '{baseFile}'", ex);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
