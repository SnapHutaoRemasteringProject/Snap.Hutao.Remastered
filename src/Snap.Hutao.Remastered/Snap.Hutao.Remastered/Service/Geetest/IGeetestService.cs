// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;
using Snap.Hutao.Remastered.Web.Hutao.Geetest;

namespace Snap.Hutao.Remastered.Service.Geetest;

public interface IGeetestService
{
    ValueTask<GeetestData?> TryVerifyGtChallengeAsync(string gt, string challenge, bool isOversea, CancellationToken token = default);

    ValueTask<string?> TryVerifyXrpcChallengeAsync(Model.Entity.User user, CardVerificationHeaders headers, CancellationToken token = default);

    ValueTask<bool> TryVerifyAigisSessionAsync(IAigisProvider provider, string? rawSession, bool isOversea, CancellationToken token = default);
}