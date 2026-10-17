// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Immutable;
using System.IO;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Service.Git;

/// <summary>Bounded transport retries; default means confirmed transport unavailability.</summary>
public sealed class RepositoryCatalogRequest(TimeSpan? timeout = null)
{
    private readonly TimeSpan requestTimeout = timeout ?? TimeSpan.FromSeconds(8);

    public async Task<ImmutableArray<GitRepository>> FetchAsync(
        Func<CancellationToken, Task<ImmutableArray<GitRepository>>> fetch,
        CancellationToken token,
        Action<int, Exception>? reportFailure = null)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            token.ThrowIfCancellationRequested();
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(requestTimeout);
            try
            {
                return await fetch(deadline.Token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException)
            {
                reportFailure?.Invoke(attempt + 1, exception);
            }
            catch (OperationCanceledException exception) when (!token.IsCancellationRequested)
            {
                reportFailure?.Invoke(attempt + 1, exception);
            }
        }

        token.ThrowIfCancellationRequested();
        return default;
    }
}
