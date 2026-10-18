// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Abstraction;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Snap.Hutao.Remastered.Web.Request.Builder;

public sealed class HttpRequestMessageBuilder :
    IBuilder,
    IHttpRequestMessageBuilder,
    IHttpHeadersBuilder<HttpHeaders>,
    IHttpHeadersBuilder<HttpRequestHeaders>,
    IHttpHeadersBuilder<HttpContentHeaders>,
    IHttpContentBuilder,
    IHttpContentHeadersBuilder,
    IHttpRequestOptionsBuilder,
    IHttpProtocolVersionBuilder,
    IRequestUriBuilder,
    IHttpMethodBuilder
{
    private HttpRequestMessageSnapshot? retrySnapshot;

    public HttpRequestMessageBuilder(IServiceProvider serviceProvider, HttpContentSerializer httpContentSerializer, HttpRequestMessage? httpRequestMessage = default)
    {
        ServiceProvider = serviceProvider;
        HttpContentSerializer = httpContentSerializer;
        HttpRequestMessage = httpRequestMessage ?? new();
    }

    public HttpRequestMessage HttpRequestMessage
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    public IServiceProvider ServiceProvider { get; }

    public HttpContentSerializer HttpContentSerializer { get; }

    HttpContentHeaders IHttpHeadersBuilder<HttpContentHeaders>.Headers
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Content);
            return Content.Headers;
        }
    }

    HttpHeaders IHttpHeadersBuilder<HttpHeaders>.Headers
    {
        get => Headers;
    }

    public HttpRequestHeaders Headers
    {
        get => HttpRequestMessage.Headers;
    }

    public HttpRequestOptions Options
    {
        get => HttpRequestMessage.Options;
    }

    public HttpContent? Content
    {
        get => HttpRequestMessage.Content;
        set => HttpRequestMessage.Content = value;
    }

    public Version Version
    {
        get => HttpRequestMessage.Version;
        set => HttpRequestMessage.Version = value;
    }

    public Uri? RequestUri
    {
        get => HttpRequestMessage.RequestUri;
        set => HttpRequestMessage.RequestUri = value;
    }

    public HttpMethod Method
    {
        get => HttpRequestMessage.Method;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            HttpRequestMessage.Method = value;
        }
    }

    public override string ToString()
    {
        return HttpRequestMessage.ToString();
    }

    public async ValueTask PrepareForRetryAsync(CancellationToken token)
    {
        // Capture before SendAsync's context disposes the original request.
        retrySnapshot = await HttpRequestMessageSnapshot.CreateAsync(HttpRequestMessage, token).ConfigureAwait(false);
    }

    public HttpRequestMessageBuilder RecreateRequest()
    {
        HttpRequestMessageSnapshot snapshot = retrySnapshot ?? throw new InvalidOperationException("PrepareForRetryAsync must be called before sending a replayable request.");
        HttpRequestMessage.Dispose();
        HttpRequestMessage = snapshot.CreateRequest();
        return this;
    }
}
