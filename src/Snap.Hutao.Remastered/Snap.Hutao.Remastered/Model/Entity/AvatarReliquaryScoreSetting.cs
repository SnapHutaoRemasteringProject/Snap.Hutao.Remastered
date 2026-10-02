// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

/// <summary>
/// 单个角色在「我的角色」页面使用的圣遗物评分算法，未保存设置的角色默认使用
/// <see cref="AvatarReliquaryScoreAlgorithm.HoyolabRecommend"/>
/// </summary>
[Table("avatar_reliquary_score_setting")]
public sealed class AvatarReliquaryScoreSetting
{
    [Key]
    public uint AvatarId { get; set; }

    public AvatarReliquaryScoreAlgorithm Algorithm { get; set; } = AvatarReliquaryScoreAlgorithm.HoyolabRecommend;

    /// <summary>
    /// <see cref="AvatarReliquaryScoreAlgorithm.Preset"/> 时生效
    /// </summary>
    public ReliquaryScoreConfigPreset PresetKey { get; set; } = ReliquaryScoreConfigPreset.Default;

    /// <summary>
    /// <see cref="AvatarReliquaryScoreAlgorithm.SavedConfig"/> 时生效，指向 <see cref="BackpackReliquaryScoreConfig.InnerId"/>
    /// </summary>
    public Guid ConfigId { get; set; }
}
