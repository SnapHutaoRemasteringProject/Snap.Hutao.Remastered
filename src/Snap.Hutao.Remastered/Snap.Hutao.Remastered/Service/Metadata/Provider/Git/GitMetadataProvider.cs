// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Service.Git;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Git;

/// <summary>
/// JSON metadata read from a shallow clone of the Snap.Metadata repository. This is the original
/// <see cref="MetadataService"/> implementation, now one source behind <see cref="IMetadataProvider"/>.
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IMetadataProvider), Key = MetadataSourceKind.Git)]
public sealed partial class GitMetadataProvider : MetadataProviderBase, IGitMetadataProvider
{
    private readonly IGitRepositoryService gitRepositoryService;
    private readonly GitMetadataProviderOptions providerOptions;
    private readonly JsonSerializerOptions options;

    private volatile string repositoryDirectory = string.Empty;

    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial GitMetadataProvider(IServiceProvider serviceProvider);

    public override MetadataSourceKind Kind { get => MetadataSourceKind.Git; }

    public string RepositoryDirectory { get => repositoryDirectory; }

    public override async ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
    {
        Verify.Operation(IsInitialized, SH.ServiceMetadataNotInitialized);

        string cacheKey = Cache.CreateKey(category, nameof(ImmutableArray<T>));

        using SentrySpanScope span = SentrySpanScope.StartChild("metadata.load", category.Name);
        span
            .SetData("metadata.file", category.Name)
            .SetData("metadata.scattered", category.IsScattered);

        if (Cache.TryGet(cacheKey, out object? value))
        {
            span.SetData("metadata.cache_hit", true);
            return Unsafe.Unbox<ImmutableArray<T>>(value);
        }

        span.SetData("metadata.cache_hit", false);

        // Cached filesystem reads/deserialization can complete synchronously; keep that CPU work off
        // the caller's thread (usually the UI thread for a page load).
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        int generation = Cache.Generation;
        ImmutableArray<T> result = category.IsScattered
            ? await ReadScatteredAsync<T>(category, token).ConfigureAwait(false)
            : await ReadSingleFileAsync<T>(category, token).ConfigureAwait(false);

        Cache.SetIfCurrent(generation, cacheKey, result);
        return result;
    }

    protected override async ValueTask<bool> InitializeCoreAsync(CancellationToken token)
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("metadata.git.repository", "metadata");
        span.SetData("metadata.repository", providerOptions.RepositoryName);

        (bool isOk, ValueDirectory directory) = await gitRepositoryService.EnsureRepositoryAsync(providerOptions.RepositoryName, token).ConfigureAwait(false);
        if (isOk)
        {
            repositoryDirectory = directory;
        }

        return isOk;
    }

    private async ValueTask<ImmutableArray<T>> ReadSingleFileAsync<T>(MetadataCategory category, CancellationToken token)
        where T : class
    {
        string path = providerOptions.GetLocalizedLocalPath($"{category.Name}.json");
        if (!File.Exists(path))
        {
            FileNotFoundException exception = new(SH.ServiceMetadataFileNotFound, category.Name);
            throw HutaoException.Throw(SH.ServiceMetadataFileNotFound, exception);
        }

        using Stream fileStream = File.OpenRead(path);
        try
        {
            return await JsonSerialize.DeserializeAsync<ImmutableArray<T>>(fileStream, options, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ex.Data["FileName"] = category.Name;
            throw;
        }
    }

    private async ValueTask<ImmutableArray<T>> ReadScatteredAsync<T>(MetadataCategory category, CancellationToken token)
        where T : class
    {
        string path = providerOptions.GetLocalizedLocalPath(category.Name);
        if (!Directory.Exists(path))
        {
            DirectoryNotFoundException exception = new(SH.ServiceMetadataFileNotFound);
            throw HutaoException.Throw(SH.ServiceMetadataFileNotFound, exception);
        }

        ImmutableArray<T>.Builder results = ImmutableArray.CreateBuilder<T>();
        foreach (string file in Directory.GetFiles(path, "*.json"))
        {
            string fileName = $"{category.Name}/{Path.GetFileNameWithoutExtension(file)}";
            using Stream fileStream = File.OpenRead(file);
            try
            {
                T? result = await JsonSerialize.DeserializeAsync<T>(fileStream, options, token).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(result);
                results.Add(result);
            }
            catch (Exception ex)
            {
                ex.Data["FileName"] = fileName;
                throw;
            }
        }

        return results.ToImmutable();
    }
}
