// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.RateLimiting;

namespace Snap.Hutao.Remastered.Service.Game.Package.Advanced;

public readonly struct GamePackageServiceContext
{
    public readonly GamePackageOperationContext Operation;
    public readonly GamePackageOperationInfo Information;
    public readonly IProgress<GamePackageOperationReport> Progress;
    public readonly ParallelOptions ParallelOptions;
    public readonly ConcurrentDictionary<string, Void> DuplicatedChunkNames = [];
    public readonly HttpClient HttpClient;
    public readonly TokenBucketRateLimiter? StreamCopyRateLimiter;
    public readonly ConcurrentDictionary<string, Void> DownloadedPatches = [];

    private readonly AsyncKeyedLock<string> chunkLocks = new();
    private readonly AsyncManualResetEvent operationResumeEvent;

    public GamePackageServiceContext(GamePackageOperationContext operation, GamePackageOperationInfo information, IProgress<GamePackageOperationReport> progress, ParallelOptions parallelOptions, HttpClient httpClient, TokenBucketRateLimiter? rateLimiter, AsyncManualResetEvent operationResumeEvent)
    {
        Operation = operation;
        Information = information;
        Progress = progress;
        ParallelOptions = parallelOptions;
        HttpClient = httpClient;
        StreamCopyRateLimiter = rateLimiter;
        this.operationResumeEvent = operationResumeEvent;
    }

    public CancellationToken CancellationToken { get => ParallelOptions.CancellationToken; }

    public async ValueTask WaitForExecutionAsync()
    {
        CancellationToken token = CancellationToken;
        token.ThrowIfCancellationRequested();
        await operationResumeEvent.WaitAsync().WaitAsync(token).ConfigureAwait(false);
    }

    public string EnsureAssetTargetDirectoryExists(string assetName)
    {
        if (Operation.Kind is GamePackageOperationKind.ExtractBlocks or GamePackageOperationKind.ExtractExecutable)
        {
            assetName = Path.GetFileName(assetName);
        }

        string targetPath = Path.Combine(Operation.EffectiveGameDirectory, assetName);

        string? directory = Path.GetDirectoryName(targetPath);
        ArgumentNullException.ThrowIfNull(directory);
        Directory.CreateDirectory(directory);

        return targetPath;
    }

    [SuppressMessage("", "SH003")]
    public Task<AsyncKeyedLock<string>.Releaser> ExclusiveProcessChunkAsync(string chunkName, CancellationToken token)
    {
        return chunkLocks.LockAsync(chunkName);
    }
}