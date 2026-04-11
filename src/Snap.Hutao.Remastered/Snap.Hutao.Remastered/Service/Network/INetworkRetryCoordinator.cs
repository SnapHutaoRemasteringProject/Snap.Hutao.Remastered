// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Network;

public interface INetworkRetryCoordinator
{
    IDisposable Register(string key, Func<CancellationToken, ValueTask<bool>> retryAsync);

    void MarkPending(string key, string warningMessage);

    void ClearPending(string key);
}
