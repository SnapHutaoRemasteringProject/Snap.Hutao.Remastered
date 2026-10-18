// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Request.Builder;

/// <summary>Buffers a replayable request before its first send/disposal using public APIs.</summary>
public sealed class HttpRequestMessageSnapshot
{
    private readonly HttpMethod method;
    private readonly Uri? requestUri;
    private readonly Version version;
    private readonly HttpVersionPolicy versionPolicy;
    private readonly KeyValuePair<string, string[]>[] headers;
    private readonly KeyValuePair<string, object?>[] options;
    private readonly byte[]? content;
    private readonly KeyValuePair<string, string[]>[] contentHeaders;

    private HttpRequestMessageSnapshot(HttpRequestMessage request, byte[]? content)
    {
        method = request.Method;
        requestUri = request.RequestUri;
        version = request.Version;
        versionPolicy = request.VersionPolicy;
        headers = request.Headers.Select(static pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value.ToArray())).ToArray();
        options = request.Options.ToArray();
        this.content = content;
        contentHeaders = request.Content?.Headers.Select(static pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value.ToArray())).ToArray() ?? [];
    }

    public static async ValueTask<HttpRequestMessageSnapshot> CreateAsync(HttpRequestMessage request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        byte[]? content = request.Content is { } body ? await body.ReadAsByteArrayAsync(token).ConfigureAwait(false) : null;
        return new(request, content);
    }

    public HttpRequestMessage CreateRequest()
    {
        HttpRequestMessage request = new(method, requestUri) { Version = version, VersionPolicy = versionPolicy };
        foreach ((string name, string[] values) in headers) request.Headers.TryAddWithoutValidation(name, values);
        foreach ((string name, object? value) in options) request.Options.Set(new HttpRequestOptionsKey<object?>(name), value);
        if (content is not null)
        {
            request.Content = new ByteArrayContent(content);
            foreach ((string name, string[] values) in contentHeaders) request.Content.Headers.TryAddWithoutValidation(name, values);
        }

        return request;
    }
}
