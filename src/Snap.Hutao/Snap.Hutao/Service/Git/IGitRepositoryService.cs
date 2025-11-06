// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO;

namespace Snap.Hutao.Service.Git;

internal interface IGitRepositoryService
{
    ValueTask<ValueResult<bool, ValueDirectory>> EnsureRepositoryAsync(string name);
}