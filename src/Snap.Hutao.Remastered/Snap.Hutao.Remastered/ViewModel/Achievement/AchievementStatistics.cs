// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Achievement;

public sealed partial class AchievementStatistics : ObservableObject
{
    public required string DisplayName { get; init => SetProperty(ref field, value); }

    public required string FinishDescription { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<AchievementView> Achievements { get; init => SetProperty(ref field, value); }

    /// <summary>
    /// 格式化完成进度
    /// "xxx/yyy - z.zz%"
    /// </summary>
    /// <param name="finished">完成的成就个数</param>
    /// <param name="totalCount">总个数</param>
    /// <param name="finishedPercent">完成进度</param>
    /// <returns>格式化的完成进度</returns>
    public static string Format(int finished, int totalCount, out double finishedPercent)
    {
        finishedPercent = (double)finished / totalCount;
        return $"{finished}/{totalCount} - {finishedPercent:P2}";
    }

    /// <summary>
    /// 格式化原石进度
    /// "xxx/yyy"
    /// </summary>
    /// <param name="finished">完成的成就获得的原石数量</param>
    /// <param name="totalCount">总原石个数</param>
    /// <returns>格式化的原石进度</returns>
    public static string FormatItem201(uint finished, uint totalCount)
    {
        return $"{finished}/{totalCount}";
    }
}
