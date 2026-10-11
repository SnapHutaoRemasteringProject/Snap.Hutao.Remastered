// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web;
using System.Net;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Core.IO.Http;

/// <summary>
/// Global redirect handler.
/// Since SocketsHttpHandler.AllowAutoRedirect=false,
/// this handler manually follows all 3xx responses, rewriting domains as needed.
/// Reuses the original HttpRequestMessage to preserve HttpCompletionOption and other properties.
/// </summary>
[Service(ServiceLifetime.Transient)]
public sealed partial class ServerDomainRedirectHandler : DelegatingHandler
{
    private const int MaxRedirects = 5;

    private readonly IServerDomainService serverDomain;

    public ServerDomainRedirectHandler(IServerDomainService serverDomain)
    {
        this.serverDomain = serverDomain;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        int redirectCount = 0;
        while (IsRedirectStatusCode(response.StatusCode) && redirectCount < MaxRedirects)
        {
            Uri? location = response.Headers.Location;
            if (location is null || !location.IsAbsoluteUri)
            {
                return response;
            }

            // Rewrite domain if needed (backup ↔ primary)
            Uri target = RewriteDomainIfNeeded(location);

            response.Dispose();

            // 301/302/303 → GET + clear body
            // 307/308 → preserve method + body
            request.RequestUri = target;
            if (response.StatusCode is not HttpStatusCode.RedirectKeepVerb and not HttpStatusCode.PermanentRedirect)
            {
                request.Method = HttpMethod.Get;
                request.Content = null;
            }

            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            redirectCount++;
        }

        return response;
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
    {
        return (int)statusCode is >= 300 and < 400;
    }

    private Uri RewriteDomainIfNeeded(Uri location)
    {
        return serverDomain.IsBackupMode()
            ? serverDomain.RewriteHostToCurrentMode(location) ?? location
            : location;
    }
}
