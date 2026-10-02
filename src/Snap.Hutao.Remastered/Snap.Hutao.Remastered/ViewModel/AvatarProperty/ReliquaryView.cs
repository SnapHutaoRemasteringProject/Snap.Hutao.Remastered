// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

/// <remarks>
/// 不能继承 <c>ObservableObject</c>：基类链的根是 <see cref="Model.NameDescription"/>，故手写 <see cref="INotifyPropertyChanged"/>，
/// 以支持「我的角色」页面切换评分算法后就地刷新分数
/// </remarks>
public sealed class ReliquaryView : EquipView, INotifyPropertyChanged
{
    private double scoreValue;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ImmutableArray<ReliquaryComposedSubProperty> ComposedSubProperties { get; set; }

    public string SetName { get; set; } = default!;

    public double ScoreValue { get => scoreValue; }

    public string Score { get => string.Format(SH.ViewPageAvatarPropertyReliquaryScoreValue, scoreValue); }

    public int ScoreColorValue { get => (int)Math.Round(scoreValue); }

    public void SetScoreValue(double value)
    {
        if (scoreValue.Equals(value))
        {
            return;
        }

        scoreValue = value;
        PropertyChanged?.Invoke(this, new(nameof(ScoreValue)));
        PropertyChanged?.Invoke(this, new(nameof(Score)));
        PropertyChanged?.Invoke(this, new(nameof(ScoreColorValue)));
    }
}
