// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

[ExtendedEnum]
public enum StaticResourceQuality
{
    [LocalizationKey(nameof(SH.ViewModelGuideStaticResourceQualityRaw))]
    Original,

    [LocalizationKey(nameof(SH.ViewModelGuideStaticResourceQualityHigh))]
    High,
}