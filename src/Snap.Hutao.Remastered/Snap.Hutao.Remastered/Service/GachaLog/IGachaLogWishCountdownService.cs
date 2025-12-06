// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.GachaLog;

namespace Snap.Hutao.Remastered.Service.GachaLog;

internal interface IGachaLogWishCountdownService
{
    ValueTask<WishCountdownBundle> GetWishCountdownBundleAsync(GachaLogWishCountdownServiceMetadataContext context);
}