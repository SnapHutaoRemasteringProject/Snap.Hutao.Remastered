// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Game.Package;
using Snap.Hutao.Remastered.Service.Game.Scheme;

namespace Snap.Hutao.Remastered.ViewModel.Game;

internal interface IViewModelSupportLaunchExecution
{
    LaunchScheme? TargetScheme { get; }

    LaunchScheme? CurrentScheme { get; }

    GameAccount? GameAccount { get; }

    ValueTask<BlockDeferral<PackageConvertStatus>> CreateConvertBlockDeferralAsync();
}