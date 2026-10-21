// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI;

internal static class AgentEndpoint
{
    public static Uri? Validate(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) { return null; }
        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out Uri? uri)
            || !(uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("The AI endpoint must use HTTPS, or HTTP on a loopback address.", nameof(endpoint));
        }
        return uri;
    }
}
