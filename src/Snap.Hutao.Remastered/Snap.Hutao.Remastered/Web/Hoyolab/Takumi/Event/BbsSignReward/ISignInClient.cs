// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.BbsSignReward;

internal interface ISignInClient
{
    ValueTask<Response<SignInRewardInfo>> GetInfoAsync(UserAndUid userAndUid, CancellationToken token = default);

    ValueTask<Response<Reward>> GetRewardAsync(Model.Entity.User user, CancellationToken token = default);

    ValueTask<Response<SignInResult>> SignAsync(UserAndUid userAndUid, CancellationToken token = default);

    ValueTask<Response<SignInRewardReSignInfo>> GetResignInfoAsync(UserAndUid userAndUid, CancellationToken token = default);

    ValueTask<Response<SignInResult>> ReSignAsync(UserAndUid userAndUid, CancellationToken token = default);
}