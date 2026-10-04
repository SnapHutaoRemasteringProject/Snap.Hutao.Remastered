// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.User;

public interface IAutoSignInService
{
    ValueTask InitializeAsync(UserAndUid userAndUid, CancellationToken token = default);

    ValueTask RunOnceAsync(UserAndUid userAndUid, CancellationToken token = default);

    ValueTask RunForAllUsersAsync(CancellationToken token = default);

    bool IsEnabled { get; set; }
}
