// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO.Hashing;
using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Wire;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// The proto bundle source: ensures the bundle file, decodes it once and maps categories on demand.
/// </summary>
/// <remarks>
/// The parsed <see cref="LanguageMetadata"/> is the L1 cache; mapped arrays and their projections are
/// L2/L3 in <see cref="MetadataProviderBase.Cache"/>. Once every category has been mapped once, L1 is
/// released; a later read decodes the file again (only after a cache clear) instead of keeping the
/// whole object graph alive for the process lifetime.
/// </remarks>
[Service(ServiceLifetime.Singleton, typeof(IMetadataProvider), Key = MetadataSourceKind.Proto)]
public sealed partial class ProtoMetadataProvider : MetadataProviderBase, IProtoMetadataProvider
{
    private readonly IMetadataBundleSource bundleSource;
    private readonly MetadataBundleOptions bundleOptions;
    private readonly CultureOptions cultureOptions;
    private readonly AsyncLock languageLock = new();

    private LanguageMetadata? language;
    private MetadataBundleInfo? bundleInfo;
    private volatile string bundleFilePath = string.Empty;

    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial ProtoMetadataProvider(IServiceProvider serviceProvider);

    public override MetadataSourceKind Kind
    {
        get => MetadataSourceKind.Proto;
    }

    public MetadataBundleInfo? BundleInfo
    {
        get => bundleInfo;
    }

    public string BundleFilePath
    {
        get => bundleFilePath;
    }

    public override async ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
    {
        Verify.Operation(IsInitialized, SH.ServiceMetadataNotInitialized);

        IProtoCategoryAdapter adapter = ProtoCategoryTable.GetAdapter(category)
            ?? throw HutaoException.InvalidOperation($"No proto adapter is registered for category '{category.ProtoName}'");

        if (adapter.ElementType != typeof(T))
        {
            throw HutaoException.InvalidOperation($"Proto category '{category.ProtoName}' maps to {adapter.ElementType.Name}, not {typeof(T).Name}");
        }

        string cacheKey = Cache.CreateKey(category, nameof(ImmutableArray<T>));
        using SentrySpanScope span = SentrySpanScope.StartChild("metadata.category.map", category.Name);

        if (Cache.TryGet(cacheKey, out object? cached) && cached is ImmutableArray<T> cachedArray)
        {
            span.SetData("metadata.cache_hit", true);
            return cachedArray;
        }

        span.SetData("metadata.cache_hit", false);

        ImmutableArray<T> result = await Cache.GetOrCreateAsync(cacheKey, async innerToken =>
        {
            LanguageMetadata metadata = await GetLanguageAsync(innerToken).ConfigureAwait(false);

            // Mapping is pure CPU work; never run it on the caller's (usually UI) thread.
            return (ImmutableArray<T>)await Task.Run(() => adapter.Read(metadata), innerToken).ConfigureAwait(false);
        }, token).ConfigureAwait(false);

        span.SetData("metadata.category.count", result.Length);
        return result;
    }

    public override void ClearCache()
    {
        // Keep the parsed LanguageMetadata: the local bundle file does not change while the process
        // runs, so re-decoding on every clear only adds latency. A replaced bundle is picked up by
        // restarting the app (or by the future metadata reload command).
        base.ClearCache();
    }

    protected override async ValueTask<bool> InitializeCoreAsync(CancellationToken token)
    {
        ProtoCategoryTable.EnsureCoverage();

        string? path = await bundleSource.EnsureLocalBundleAsync(token).ConfigureAwait(false);
        if (path is null)
        {
            Logger.LogWarning("No metadata bundle is available locally or remotely");
            return false;
        }

        MetadataBundle bundle;
        try
        {
            bundle = await Task.Run(() => MetadataBundleCodec.Read<MetadataBundle>(path, MetadataProtoModel.Instance, token), token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A file that cannot be decoded is unusable; removing it lets the next attempt download
            // a fresh copy instead of failing forever. An override path is never deleted.
            Logger.LogWarning(exception, "Metadata bundle could not be decoded");
            TryDeleteCorruptBundle(path);
            return false;
        }

        LanguageMetadata? languageMetadata = ValidateBundle(bundle);
        if (languageMetadata is null)
        {
            return false;
        }

        bundleFilePath = path;
        language = languageMetadata;
        bundleInfo = await CreateBundleInfoAsync(bundle, path, token).ConfigureAwait(false);

        Logger.LogInformation(
            "Metadata bundle {Version} ({Language}) initialized from {Size} bytes",
            bundleInfo.Version,
            bundleInfo.Language,
            bundleInfo.Size);

        return true;
    }

    protected override async ValueTask ResetCoreAsync(CancellationToken token)
    {
        using (await languageLock.LockAsync().ConfigureAwait(false))
        {
            language = null;
            bundleInfo = null;
            bundleFilePath = string.Empty;
        }
    }

    private async ValueTask<LanguageMetadata> GetLanguageAsync(CancellationToken token)
    {
        if (language is { } cached)
        {
            return cached;
        }

        using (await languageLock.LockAsync().ConfigureAwait(false))
        {
            if (language is { } recheckedLanguage)
            {
                return recheckedLanguage;
            }

            string path = bundleFilePath;
            if (string.IsNullOrEmpty(path))
            {
                throw HutaoException.InvalidOperation(SH.ServiceMetadataNotInitialized);
            }

            MetadataBundle bundle = await Task.Run(() => MetadataBundleCodec.Read<MetadataBundle>(path, MetadataProtoModel.Instance, token), token).ConfigureAwait(false);
            LanguageMetadata validated = ValidateBundle(bundle) ?? throw HutaoException.InvalidOperation(SH.ServiceMetadataVersionNotSupported);

            if (bundleInfo is null)
            {
                bundleInfo = await CreateBundleInfoAsync(bundle, path, token).ConfigureAwait(false);
            }

            language = validated;
            return validated;
        }
    }

    private LanguageMetadata? ValidateBundle(MetadataBundle bundle)
    {
        if (bundle.Version is { Major: <= 0 })
        {
            Logger.LogWarning("Metadata bundle does not declare a game version");
        }

        if (string.IsNullOrEmpty(bundle.Info?.Publisher))
        {
            Logger.LogInformation("Metadata bundle does not declare a publisher");
        }

        if (bundle.Languages.Length is not 1)
        {
            Logger.LogWarning("Metadata bundle must contain exactly one language, found {Count}", bundle.Languages.Length);
            return null;
        }

        LanguageMetadata languageMetadata = bundle.Languages[0];
        if (!string.Equals(languageMetadata.Language, cultureOptions.LocaleName, StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("Metadata bundle language does not match the current locale");
            return null;
        }

        return languageMetadata;
    }

    private async ValueTask<MetadataBundleInfo> CreateBundleInfoAsync(MetadataBundle bundle, string path, CancellationToken token)
    {
        FileInfo file = new(path);
        string hash = await XxHash64.HashFileAsync(path, token).ConfigureAwait(false);

        return new()
        {
            Publisher = bundle.Info?.Publisher ?? string.Empty,
            VersionMajor = bundle.Version?.Major ?? 0,
            VersionMinor = bundle.Version?.Minor ?? 0,
            VersionPatch = bundle.Version?.Patch ?? 0,
            GeneratedAt = DateTimeOffset.TryParse(bundle.Info?.GeneratedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset generatedAt)
                ? generatedAt
                : default,

            // The producer does not write schema_version into the bundle yet; the value stays 0 until
            // the producer-side versioning contract lands (docs/proto-metadata.md 搂10.1).
            SchemaVersion = 0,
            Language = bundle.Languages[0].Language,
            Hash = hash,
            Size = file.Length,
            Path = path,
        };
    }

    private void TryDeleteCorruptBundle(string path)
    {
        if (string.Equals(path, bundleOptions.BundlePath, StringComparison.OrdinalIgnoreCase) is false)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Logger.LogWarning(exception, "Failed to delete a corrupt metadata bundle");
        }
    }
}

