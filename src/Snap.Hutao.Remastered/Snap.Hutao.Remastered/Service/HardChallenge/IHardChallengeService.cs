// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.HardChallenge;
using Snap.Hutao.Remastered.ViewModel.User;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.HardChallenge;

internal interface IHardChallengeService
{
    ValueTask<ObservableCollection<HardChallengeView>> GetHardChallengeViewCollectionAsync(HardChallengeMetadataContext context, UserAndUid userAndUid);

    ValueTask<ImmutableArray<AvatarView>> GetBlingAvatarsAsync(HardChallengeMetadataContext context, UserAndUid userAndUid);

    ValueTask RefreshHardChallengeAsync(HardChallengeMetadataContext context, UserAndUid userAndUid);
}