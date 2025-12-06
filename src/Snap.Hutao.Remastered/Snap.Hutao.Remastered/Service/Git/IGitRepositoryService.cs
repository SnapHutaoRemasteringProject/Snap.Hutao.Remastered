// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;

namespace Snap.Hutao.Remastered.Service.Git;

internal interface IGitRepositoryService
{
    ValueTask<ValueResult<bool, ValueDirectory>> EnsureRepositoryAsync(string name);
}