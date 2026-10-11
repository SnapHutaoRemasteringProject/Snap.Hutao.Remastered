// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Web;
using Snap.Hutao.Remastered.Web.Request.Builder;
using System.Net.Http;
using System.Runtime.ExceptionServices;

namespace Snap.Hutao.Remastered.Core.IO.Http;

[Service(ServiceLifetime.Transient)]
public sealed partial class RetryHttpHandler : DelegatingHandler
{
    private const int MaxAttemptCount = 3;
    private const int BaseRetryDelayMilliseconds = 200;

    private readonly IServerDomainService serverDomain;

    public RetryHttpHandler(IServerDomainService serverDomain)
    {
        this.serverDomain = serverDomain;
    }

    public static HttpRequestOptionsKey<bool> DisableRetry { get; } = new("DisableRetry");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(DisableRetry, out bool skipRetry) && skipRetry)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Only idempotent methods may be replayed. Retrying a non idempotent request
        // (sign-in, gacha log upload, ...) can apply the operation twice on the server.
        bool retryable = IsIdempotentMethod(request.Method);

        ExceptionDispatchInfo? dispatch = default;
        for (int attempt = 0; attempt < MaxAttemptCount; attempt++)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                // Retry on server error status codes (5xx) such as 502 Bad Gateway
                if ((int)response.StatusCode >= 500)
                {
                    response.EnsureSuccessStatusCode();
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                response?.Dispose();
                dispatch = ExceptionDispatchInfo.Capture(ex);

                if (!retryable || attempt == MaxAttemptCount - 1)
                {
                    break;
                }

                PrepareForRetry(request, ex);

                // Exponential backoff with jitter: ~200ms, ~400ms
                await Task.Delay(GetRetryDelay(attempt + 1), cancellationToken).ConfigureAwait(false);
            }
        }

        dispatch?.Throw();
        throw HutaoException.InvalidOperation("Unexpected request retry state");
    }

    private static bool IsIdempotentMethod(HttpMethod method)
    {
        return method == HttpMethod.Get
            || method == HttpMethod.Head
            || method == HttpMethod.Options
            || method == HttpMethod.Put
            || method == HttpMethod.Delete;
    }

    private static TimeSpan GetRetryDelay(int retryCount)
    {
        int backoff = BaseRetryDelayMilliseconds << (retryCount - 1);
        int jitter = System.Random.Shared.Next(BaseRetryDelayMilliseconds);
        return TimeSpan.FromMilliseconds(backoff + jitter);
    }

    private void PrepareForRetry(HttpRequestMessage request, HttpRequestException exception)
    {
        // Detect SSL connection error → auto switch to backup domain + rewrite request URL
        NetworkError networkError = HttpRequestExceptionHandling.HttpRequestExceptionToNetworkError(exception);
        if (networkError is NetworkError.ERR_SECURE_CONNECTION_RESET
                         or NetworkError.ERR_SECURE_CONNECTION_ERROR
                         or NetworkError.ERR_SECURE_CONNECTION_ABORTED)
        {
            serverDomain.TryAutoFallback();

            request.RequestUri = serverDomain.RewriteHostToCurrentMode(request.RequestUri);
        }

        // The message must be marked as not yet sent before it can be sent again
        request.Resurrect();
    }
}
