// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata;

public interface IMetadataServiceInitialization
{
    ValueTask<bool> InitializeInternalAsync(CancellationToken token = default);
}