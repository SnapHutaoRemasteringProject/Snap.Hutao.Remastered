// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Core.IO.Http;

/// <summary>
/// Measures the complete HTTP exchange as a Sentry child span.
/// </summary>
/// <remarks>
/// It is the outermost handler so the span includes retries performed by
/// <see cref="RetryHttpHandler"/> and host rewrites performed by
/// <see cref="ServerDomainRedirectHandler"/>. Query strings are stripped from the span
/// description because they can carry cookies, tokens and other account data.
/// </remarks>
[Service(ServiceLifetime.Transient)]
public sealed partial class SentryTracingHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using SentrySpanScope span = SentrySpanScope.StartChild("http.client", Describe(request));

        span
            .SetData("http.request.method", request.Method.Method)
            .SetData("url.path", request.RequestUri?.AbsolutePath ?? string.Empty);

        if (request.RequestUri is { } uri)
        {
            span.SetData("server.address", uri.Authority);
        }

        try
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            span.SetData("http.response.status_code", (int)response.StatusCode);
            if (response.Content.Headers.ContentLength is { } contentLength)
            {
                span.SetData("http.response_content_length", contentLength);
            }

            return response;
        }
        catch (Exception exception)
        {
            span.SetException(exception);
            throw;
        }
    }

    private static string Describe(HttpRequestMessage request)
    {
        if (request.RequestUri is not { } uri)
        {
            return $"{request.Method.Method} <null>";
        }

        return $"{request.Method.Method} {uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
    }
}
