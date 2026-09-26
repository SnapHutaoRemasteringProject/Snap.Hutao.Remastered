// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

/// <summary>
/// 「我的角色」页面评分算法下拉选项，值相等（<see langword="record"/>），
/// 便于切换角色时通过 <see cref="object.Equals(object?)"/> 找回同一选项实例
/// </summary>
public sealed record AvatarReliquaryScoreOption(
    string Name,
    AvatarReliquaryScoreAlgorithm Algorithm,
    ReliquaryScoreConfigPreset PresetKey,
    Guid ConfigId);
