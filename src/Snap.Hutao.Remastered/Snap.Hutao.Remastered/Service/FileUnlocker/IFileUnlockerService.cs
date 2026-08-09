// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

public interface IFileUnlockerService
{
    ValueTask<LockUnlockResponse?> UnlockAsync(string directoryPath);
}
