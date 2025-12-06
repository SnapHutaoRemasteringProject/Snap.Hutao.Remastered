// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Yae.Achievement;

namespace Snap.Hutao.Remastered.Service.Feature;

internal interface IFeatureService
{
    ValueTask<AchievementFieldId?> GetAchievementFieldIdAsync(string tag);
}