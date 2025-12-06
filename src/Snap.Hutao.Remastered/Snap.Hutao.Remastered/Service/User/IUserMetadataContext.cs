// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;

namespace Snap.Hutao.Remastered.Service.User;

internal interface IUserMetadataContext : IMetadataContext,
    IMetadataArrayProfilePictureSource;
