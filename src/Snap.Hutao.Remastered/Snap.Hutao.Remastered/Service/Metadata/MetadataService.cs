// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Service.Git;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Service.Metadata;

[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]
[HttpClient(HttpClientConfiguration.Default)]
public sealed partial class MetadataService : IMetadataService
{
    private TaskCompletionSource initializeCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SemaphoreSlim initializationGate = new(1, 1);

    private readonly IGitRepositoryService gitRepositoryService;
    private readonly ILogger<MetadataService> logger;
    private readonly MetadataOptions metadataOptions;
    private readonly JsonSerializerOptions options;

    private volatile bool isInitialized;

    [GeneratedConstructor]
    public partial MetadataService(IServiceProvider serviceProvider);

    public partial IMemoryCache MemoryCache { get; }

    public async ValueTask<bool> InitializeAsync()
    {
        await initializeCompletionSource.Task.WaitAsync(XamlApplicationLifetime.ShutdownToken).ConfigureAwait(false);
        return isInitialized;
    }

    public async ValueTask<bool> InitializepublicAsync(CancellationToken token = default)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        token = linked.Token;
        await initializationGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (isInitialized) return true;
            using (ValueStopwatch.MeasureExecution(logger))
            {
                (isInitialized, _) = await gitRepositoryService.EnsureRepositoryAsync("Snap.Metadata", token).ConfigureAwait(false);
            }
            return isInitialized;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Metadata initialization failed; awaiting a later retry");
            return false;
        }
        finally
        {
            // Consumers require real metadata, not merely a completed failed
            // attempt. Keep the success signal pending across short retries;
            // ShutdownToken releases those consumers when the application exits.
            if (isInitialized) initializeCompletionSource.TrySetResult();
            initializationGate.Release();
        }
    }


    public async ValueTask<ImmutableArray<T>> FromCacheOrFileAsync<T>(MetadataFileStrategy strategy, CancellationToken token)
        where T : class
    {
        Verify.Operation(isInitialized, SH.ServiceMetadataNotInitialized);
        string cacheKey = $"{nameof(MetadataService)}.Cache.{strategy.Name}";

        if (MemoryCache.TryGetValue(cacheKey, out object? value))
        {
            ArgumentNullException.ThrowIfNull(value);
            return Unsafe.Unbox<ImmutableArray<T>>(value);
        }

        // Cached filesystem reads/deserialization can complete synchronously.
        // Keep that CPU work off the UI without changing initialization ordering.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        return strategy.IsScattered
            ? await FromCacheOrScatteredFileAsync<T>(strategy, cacheKey, token).ConfigureAwait(false)
            : await FromCacheOrSingleFileAsync<T>(strategy, cacheKey, token).ConfigureAwait(false);
    }

    private async ValueTask<ImmutableArray<T>> FromCacheOrSingleFileAsync<T>(MetadataFileStrategy strategy, string cacheKey, CancellationToken token)
        where T : class
    {
        string path = metadataOptions.GetLocalizedLocalPath($"{strategy.Name}.json");
        if (!File.Exists(path))
        {
            FileNotFoundException exception = new(SH.ServiceMetadataFileNotFound, strategy.Name);
            throw HutaoException.Throw(SH.ServiceMetadataFileNotFound, exception);
        }

        using (Stream fileStream = File.OpenRead(path))
        {
            try
            {
                ImmutableArray<T> result = await JsonSerializer.DeserializeAsync<ImmutableArray<T>>(fileStream, options, token).ConfigureAwait(false);
                return MemoryCache.Set(cacheKey, result);
            }
            catch (Exception ex)
            {
                ex.Data.Add("FileName", strategy.Name);
                throw;
            }
        }
    }

    private async ValueTask<ImmutableArray<T>> FromCacheOrScatteredFileAsync<T>(MetadataFileStrategy strategy, string cacheKey, CancellationToken token)
        where T : class
    {
        string path = metadataOptions.GetLocalizedLocalPath(strategy.Name);
        if (!Directory.Exists(path))
        {
            DirectoryNotFoundException exception = new(SH.ServiceMetadataFileNotFound);
            throw HutaoException.Throw(SH.ServiceMetadataFileNotFound, exception);
        }

        ImmutableArray<T>.Builder results = ImmutableArray.CreateBuilder<T>();
        foreach (string file in Directory.GetFiles(path, "*.json"))
        {
            string fileName = $"{strategy.Name}/{Path.GetFileNameWithoutExtension(file)}";
            using (Stream fileStream = File.OpenRead(file))
            {
                try
                {
                    T? result = await JsonSerializer.DeserializeAsync<T>(fileStream, options, token).ConfigureAwait(false);
                    ArgumentNullException.ThrowIfNull(result);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    ex.Data.Add("FileName", fileName);
                    throw;
                }
            }
        }

        return MemoryCache.Set(cacheKey, results.ToImmutable());
    }
}
