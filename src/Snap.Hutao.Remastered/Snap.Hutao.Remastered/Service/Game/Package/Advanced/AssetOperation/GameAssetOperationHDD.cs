// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32.SafeHandles;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.IO.Compression.Zstandard;
using Snap.Hutao.Remastered.Service.Game.Package.Advanced.Model;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Downloader.Proto;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Game.Package.Advanced.AssetOperation;

[SuppressMessage("", "SA1202")]
[Service(ServiceLifetime.Transient)]
public sealed partial class GameAssetOperationHDD : GameAssetOperation
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial GameAssetOperationHDD(IServiceProvider serviceProvider);

    public override async ValueTask InstallAssetsAsync(GamePackageServiceContext context, SophonDecodedBuild remoteBuild)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonDecodedManifest manifest in remoteBuild.Manifests)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            IEnumerable<SophonAssetOperation> assets = manifest.Data.Assets.Select(asset => SophonAssetOperation.AddOrRepair(manifest.UrlPrefix, manifest.UrlSuffix, asset));
            foreach (SophonAssetOperation asset in assets)
            {
                await context.WaitForExecutionAsync().ConfigureAwait(false);
                await EnsureAssetAsync(context, asset).ConfigureAwait(false);
            }
        }
    }

    public override async ValueTask UpdateDiffAssetsAsync(GamePackageServiceContext context, ImmutableArray<SophonAssetOperation> diffAssets)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonAssetOperation asset in diffAssets)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            ValueTask task = asset.Kind switch
            {
                SophonAssetOperationKind.AddOrRepair or SophonAssetOperationKind.Modify => EnsureAssetAsync(context, asset),
                SophonAssetOperationKind.Delete => DeleteAssetAsync(context, asset.OldAsset),
                _ => ValueTask.CompletedTask,
            };

            await task.ConfigureAwait(false);
        }
    }

    public override async ValueTask PredownloadDiffAssetsAsync(GamePackageServiceContext context, ImmutableArray<SophonAssetOperation> diffAssets)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonAssetOperation asset in diffAssets)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            IReadOnlyList<SophonChunk> chunks = asset.Kind switch
            {
                SophonAssetOperationKind.AddOrRepair => [.. asset.NewAsset.AssetChunks.Select(c => new SophonChunk(asset.UrlPrefix, asset.UrlSuffix, c))],
                SophonAssetOperationKind.Modify => asset.DiffChunks,
                _ => [],
            };

            await DownloadChunksAsync(context, chunks).ConfigureAwait(false);
        }
    }

    protected override async ValueTask VerifyManifestsAsync(GamePackageServiceContext context, SophonDecodedBuild build, Action<SophonAssetOperation> conflictHandler)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonDecodedManifest manifest in build.Manifests)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            await VerifyManifestAsync(context, manifest, conflictHandler).ConfigureAwait(false);
        }
    }

    protected override async ValueTask VerifyManifestAsync(GamePackageServiceContext context, SophonDecodedManifest manifest, Action<SophonAssetOperation> conflictHandler)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (AssetProperty asset in manifest.Data.Assets)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            await VerifyAssetAsync(context, new(manifest.UrlPrefix, manifest.UrlSuffix, asset), conflictHandler).ConfigureAwait(false);
        }
    }

    protected override async ValueTask RepairAssetsAsync(GamePackageServiceContext context, GamePackageIntegrityInfo info)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonAssetOperation asset in info.ConflictedAssets)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            await EnsureAssetAsync(context, asset).ConfigureAwait(false);
        }
    }

    protected override async ValueTask DownloadChunksAsync(GamePackageServiceContext context, IReadOnlyList<SophonChunk> sophonChunks)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonChunk chunk in sophonChunks)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            await DownloadChunkAsync(context, chunk).ConfigureAwait(false);
        }
    }

    protected override async ValueTask MergeNewAssetAsync(GamePackageServiceContext context, AssetProperty assetProperty)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        CancellationToken token = context.CancellationToken;

        string path = context.EnsureAssetTargetDirectoryExists(assetProperty.AssetName);
        using (SafeFileHandle fileHandle = File.OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.None, preallocationSize: 32 * 1024))
        using (IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(ChunkBufferSize))
        {
            Memory<byte> buffer = memoryOwner.Memory;

            foreach (AssetChunk chunk in assetProperty.AssetChunks)
            {
                await context.WaitForExecutionAsync().ConfigureAwait(false);
                string chunkPath = Path.Combine(context.Operation.EffectiveChunksDirectory, chunk.ChunkName);
                if (!File.Exists(chunkPath))
                {
                    continue;
                }

                using (await context.ExclusiveProcessChunkAsync(chunk.ChunkName, token).ConfigureAwait(false))
                using (FileStream chunkFile = File.OpenRead(chunkPath))
                using (ZstandardDecompressStream decompressor = new(chunkFile))
                {
                    long offset = chunk.ChunkOnFileOffset;
                    do
                    {
                        await context.WaitForExecutionAsync().ConfigureAwait(false);
                        int bytesRead = await decompressor.ReadAsync(buffer, token).ConfigureAwait(false);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        await RandomAccess.WriteAsync(fileHandle, buffer[..bytesRead], offset, token).ConfigureAwait(false);
                        context.Progress.Report(new GamePackageOperationReport.Install(bytesRead, 0, chunk.ChunkName));
                        offset += bytesRead;
                    }
                    while (true);

                    if (context.Operation.Kind is GamePackageOperationKind.Install or GamePackageOperationKind.Update && !context.DuplicatedChunkNames.ContainsKey(chunk.ChunkName))
                    {
                        FileOperation.Delete(chunkPath);
                    }
                }

                context.Progress.Report(new GamePackageOperationReport.Install(0, 1, chunk.ChunkName));
            }
        }
    }

    public override async ValueTask InstallOrPatchAssetsAsync(GamePackageServiceContext context, SophonDecodedPatchBuild patchBuild)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonDecodedPatchManifest manifest in patchBuild.Manifests)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            IEnumerable<SophonPatchAsset> assets = manifest.Data.FileDatas
                .Where(fd => fd.PatchesEntries.SingleOrDefault(pe => pe.Key == manifest.OriginalTag) is not null)
                .Select(fd => new SophonPatchAsset(manifest.UrlPrefix, manifest.UrlSuffix, fd, fd.PatchesEntries.Single(pe => pe.Key == manifest.OriginalTag).PatchInfo));
            foreach (SophonPatchAsset patchAsset in assets)
            {
                await context.WaitForExecutionAsync().ConfigureAwait(false);
                await InstallOrPatchAssetAsync(context, patchAsset).ConfigureAwait(false);
            }
        }
    }

    public override async ValueTask DeletePatchDeprecatedFilesAsync(GamePackageServiceContext context, SophonDecodedPatchBuild patchBuild)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonDecodedPatchManifest manifest in patchBuild.Manifests)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            IEnumerable<string> assetNames = manifest.Data.DeleteFilesEntries.SingleOrDefault(fd => fd.Key == manifest.OriginalTag)?.DeleteFiles.Infos.Select(i => i.Name) ?? [];
            foreach (string assetName in assetNames)
            {
                await context.WaitForExecutionAsync().ConfigureAwait(false);
                string assetPath = Path.Combine(context.Operation.EffectiveGameDirectory, assetName);
                if (File.Exists(assetPath))
                {
                    File.Delete(assetPath);
                }
            }
        }
    }

    public override async ValueTask PredownloadPatchesAsync(GamePackageServiceContext context, SophonDecodedPatchBuild patchBuild)
    {
        await context.WaitForExecutionAsync().ConfigureAwait(false);
        foreach (SophonDecodedPatchManifest manifest in patchBuild.Manifests)
        {
            await context.WaitForExecutionAsync().ConfigureAwait(false);
            IEnumerable<SophonPatchAsset> assets = manifest.Data.FileDatas
                .Where(fd => fd.PatchesEntries.SingleOrDefault(pe => pe.Key == manifest.OriginalTag) is not null)
                .Select(fd => new SophonPatchAsset(manifest.UrlPrefix, manifest.UrlSuffix, fd, fd.PatchesEntries.Single(pe => pe.Key == manifest.OriginalTag).PatchInfo));
            foreach (SophonPatchAsset patchAsset in assets)
            {
                await context.WaitForExecutionAsync().ConfigureAwait(false);
                await DownloadPatchAsync(context, patchAsset).ConfigureAwait(false);
            }
        }
    }
}