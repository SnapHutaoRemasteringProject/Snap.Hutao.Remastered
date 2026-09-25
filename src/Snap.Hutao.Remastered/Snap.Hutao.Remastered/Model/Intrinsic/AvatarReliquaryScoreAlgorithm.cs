// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Intrinsic;

/// <summary>
/// 「我的角色」页面单件圣遗物评分的算法来源
/// </summary>
public enum AvatarReliquaryScoreAlgorithm
{
    /// <summary>
    /// 按米游社返回的推荐副属性评分
    /// </summary>
    HoyolabRecommend,

    /// <summary>
    /// 按内置评分配置预设评分
    /// </summary>
    Preset,

    /// <summary>
    /// 按背包中保存的评分配置评分
    /// </summary>
    SavedConfig,
}
