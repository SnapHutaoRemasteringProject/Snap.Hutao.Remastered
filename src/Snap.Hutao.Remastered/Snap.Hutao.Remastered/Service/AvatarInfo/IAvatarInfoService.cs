// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.AvatarInfo.Factory;
using Snap.Hutao.Remastered.ViewModel.AvatarProperty;
using Snap.Hutao.Remastered.ViewModel.User;

namespace Snap.Hutao.Remastered.Service.AvatarInfo;

internal interface IAvatarInfoService
{
    ValueTask<Summary?> GetSummaryAsync(SummaryFactoryMetadataContext context, UserAndUid userAndUid, RefreshOptionKind refreshOptionKind, CancellationToken token = default);
}