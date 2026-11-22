// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text;

namespace Snap.Hutao.Remastered.Service.Metadata;

/// <summary>
/// One metadata provider's derived cache (category arrays and their projection dictionaries).
/// Every key is prefixed with the provider kind and the current locale, and every write is guarded
/// by a generation counter so a <see cref="Clear"/> cannot be undone by a load that started before it.
/// </summary>
/// <remarks>
/// The prefix is <c>Metadata.{Git|Proto}.{locale}</c>. This type only ever touches keys it created,
/// so clearing metadata never removes entries owned by other <see cref="IMemoryCache"/> users.
/// </remarks>
public sealed class MetadataCache
{
    private readonly IMemoryCache cache;
    private readonly ConcurrentDictionary<string, byte> keys = new(StringComparer.Ordinal);
    private readonly AsyncKeyedLock<string> creationLocks = new(StringComparer.Ordinal);

    private int generation;

    public MetadataCache(IMemoryCache cache, string prefix)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        this.cache = cache;
        Prefix = prefix;
    }

    public string Prefix { get; }

    public int Generation => Volatile.Read(ref generation);

    /// <summary>
    /// Builds a cache key: <c>{prefix}.{category}.{shape}[.{parts…}]</c>.
    /// </summary>
    public string CreateKey(MetadataCategory category, string shape, params ReadOnlySpan<string> parts)
    {
        StringBuilder builder = new(Prefix.Length + category.Name.Length + shape.Length + 16);
        builder
            .Append(Prefix)
            .Append('.').Append(category.Name)
            .Append('.').Append(shape);

        foreach (string part in parts)
        {
            builder.Append('.').Append(part);
        }

        return builder.ToString();
    }

    public bool TryGet(string key, [NotNullWhen(true)] out object? value)
    {
        // The cache never stores null, so treating null as a miss keeps the annotation honest.
        if (cache.TryGetValue(key, out value) && value is not null)
        {
            keys.TryAdd(key, 0);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Writes <paramref name="value"/> only when the cache has not been cleared since the load
    /// captured <paramref name="expectedGeneration"/>. A result loaded before a clear may still be
    /// returned to its caller, but must not be cached for the next generation.
    /// </summary>
    public void SetIfCurrent(int expectedGeneration, string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (Generation != expectedGeneration)
        {
            return;
        }

        cache.Set(key, value);
        keys.TryAdd(key, 0);

        // A Clear may run between the generation check and the write above. Removing the key again
        // when the generation moved keeps the new generation clean.
        if (Generation != expectedGeneration && keys.TryRemove(key, out _))
        {
            cache.Remove(key);
        }
    }

    /// <summary>
    /// Returns the cached value or creates it, dropping the result when the cache is cleared while
    /// the factory runs.
    /// </summary>
    public async ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken token)
        where T : notnull
    {
        if (TryGet(key, out object? cached) && cached is T value)
        {
            return value;
        }

        // Serialize the expensive load per key: concurrent pages asking for the same category must
        // not each deserialize/map it. Nested projections use different keys, so this cannot deadlock.
        using (await creationLocks.LockAsync(key).ConfigureAwait(false))
        {
            if (TryGet(key, out cached) && cached is T lockedValue)
            {
                return lockedValue;
            }

            int expectedGeneration = Generation;
            T created = await factory(token).ConfigureAwait(false);
            SetIfCurrent(expectedGeneration, key, created);
            return created;
        }
    }

    /// <summary>
    /// Removes every key this cache created and advances the generation.
    /// </summary>
    /// <returns>The number of removed keys.</returns>
    public int Clear()
    {
        Interlocked.Increment(ref generation);

        int removed = 0;
        foreach (string key in keys.Keys)
        {
            if (cache.TryGetValue(key, out _))
            {
                removed++;
            }

            cache.Remove(key);
        }

        keys.Clear();
        return removed;
    }
}
