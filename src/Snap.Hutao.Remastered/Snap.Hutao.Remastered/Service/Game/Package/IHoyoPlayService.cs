// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.Scheme;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay.Connect.Branch;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay.Connect.ChannelSDK;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay.Connect.DeprecatedFile;

namespace Snap.Hutao.Remastered.Service.Game.Package;

internal interface IHoyoPlayService
{
    ValueTask<ValueResult<bool, GameBranchesWrapper>> TryGetBranchesAsync(LaunchScheme scheme);

    ValueTask<ValueResult<bool, GameChannelSDKsWrapper>> TryGetChannelSDKsAsync(LaunchScheme scheme);

    ValueTask<ValueResult<bool, DeprecatedFileConfigurationsWrapper>> TryGetDeprecatedFileConfigurationsAsync(LaunchScheme scheme);
}