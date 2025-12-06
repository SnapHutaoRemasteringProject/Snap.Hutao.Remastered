// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.FileSystem;
using Snap.Hutao.Remastered.Service.Game.Package.Advanced.Model;
using Snap.Hutao.Remastered.Web.Hoyolab.Downloader;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay.Connect.Branch;

namespace Snap.Hutao.Remastered.Service.Game.Package.Advanced;

internal interface IGamePackageService
{
    ValueTask<bool> ExecuteOperationAsync(GamePackageOperationContext context);

    ValueTask CancelOperationAsync();

    ValueTask<SophonDecodedBuild?> DecodeManifestsAsync(IGameFileSystemView gameFileSystem, BranchWrapper? branch, CancellationToken token = default);

    ValueTask<SophonDecodedBuild?> DecodeManifestsAsync(IGameFileSystemView gameFileSystem, SophonBuild? build, CancellationToken token = default);

    ValueTask<SophonDecodedPatchBuild?> DecodeDiffManifestsAsync(IGameFileSystemView gameFileSystem, BranchWrapper? branch, CancellationToken token = default);

    ValueTask<SophonDecodedPatchBuild?> DecodeDiffManifestsAsync(IGameFileSystemView gameFileSystem, SophonPatchBuild? patchBuild, CancellationToken token = default);
}