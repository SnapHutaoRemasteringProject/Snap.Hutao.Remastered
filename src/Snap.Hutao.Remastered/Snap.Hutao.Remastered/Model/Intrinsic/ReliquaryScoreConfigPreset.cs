// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Intrinsic;

[ExtendedEnum]
public enum ReliquaryScoreConfigPreset
{
    [LocalizationKey(nameof(SH.ModelIntrinsicReliquaryScoreConfigPresetDefault))]
    Default,

    [LocalizationKey(nameof(SH.ModelIntrinsicReliquaryScoreConfigPresetATKScaler))]
    ATKScaler,

    [LocalizationKey(nameof(SH.ModelIntrinsicReliquaryScoreConfigPresetHPScaler))]
    HPScaler,

    [LocalizationKey(nameof(SH.ModelIntrinsicReliquaryScoreConfigPresetDEFScaler))]
    DEFScaler,

    [LocalizationKey(nameof(SH.ModelIntrinsicReliquaryScoreConfigPresetEM))]
    EM,

    [LocalizationKey(nameof(SH.ModelIntrinsicReliquaryScoreConfigPresetCustom))]
    Custom,
}
