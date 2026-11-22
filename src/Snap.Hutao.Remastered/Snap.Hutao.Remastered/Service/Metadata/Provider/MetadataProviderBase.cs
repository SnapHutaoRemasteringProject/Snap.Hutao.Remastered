// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Snap.Hutao.Remastered.Core.Diagnostics;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider;

/// <summary>
/// Shared single-source initialization gate and cache instance. A provider reports success/failure
/// and never holds a completion source: waiting, retrying and falling back are the manager's job.
/// </summary>
public abstract class MetadataProviderBase : IMetadataProvider, IDisposable
{
    private readonly SemaphoreSlim initializationGate = new(1, 1);
    private bool isInitialized;
    private bool isDisposed;

    protected MetadataProviderBase(IServiceProvider serviceProvider)
    {
        Logger = serviceProvider.GetRequiredService<ILogger<MetadataProviderBase>>();

        // Kind is a constant of the derived type (see the note on the abstract member), so reading
        // it here during construction cannot observe an uninitialized field.
        string locale = serviceProvider.GetRequiredService<CultureOptions>().LocaleName;
        Cache = new(serviceProvider.GetRequiredService<IMemoryCache>(), $"Metadata.{Kind}.{locale}");
    }

    /// <summary>
    /// The source kind. Implementations must return a constant, never read a derived field: the
    /// base constructor evaluates this member.
    /// </summary>
    public abstract MetadataSourceKind Kind { get; }

    public MetadataCache Cache { get; }

    public bool IsInitialized => Volatile.Read(ref isInitialized);

    protected ILogger Logger { get; }

    public async ValueTask<bool> InitializeAsync(CancellationToken token = default)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        await initializationGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (IsInitialized)
            {
                return true;
            }

            using SentrySpanScope span = SentrySpanScope.StartOperation($"metadata.{Kind}.initialize", "metadata");
            span.SetData("metadata.source", Kind.ToString());

            try
            {
                isInitialized = await InitializeCoreAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // A failed attempt must not latch "initialized": the manager retries this provider
                // after a fallback or after the network recovers.
                Logger.LogWarning(exception, "Metadata provider {Kind} initialization failed", Kind);
                isInitialized = false;
            }

            span.SetData("metadata.initialized", IsInitialized);
            return IsInitialized;
        }
        finally
        {
            initializationGate.Release();
        }
    }

    public abstract ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class;

    public virtual void ClearCache()
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("metadata.cache.clear", "metadata");
        int removed = Cache.Clear();
        span
            .SetData("metadata.source", Kind.ToString())
            .SetData("metadata.cache.removed", removed);
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        initializationGate.Dispose();
    }

    /// <summary>
    /// Prepares the source. Returns false when unavailable; cancellation is thrown.
    /// </summary>
    protected abstract ValueTask<bool> InitializeCoreAsync(CancellationToken token);
}
