// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Update;

public interface IUpdateService
{
    string? UpdateInfo { get; }

    ValueTask<CheckUpdateResult> CheckUpdateAsync(CancellationToken token = default);

    ValueTask TriggerUpdateAsync(CheckUpdateResult result, bool isAutomatic = false, CancellationToken token = default);
}