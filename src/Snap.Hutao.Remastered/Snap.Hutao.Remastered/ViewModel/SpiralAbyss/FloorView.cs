// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Metadata.Tower;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.SpiralAbyss;

public sealed partial class FloorView : ObservableObject, IPropertyValuesProvider
{
    public FloorView(TowerFloor floor, SpiralAbyssMetadataContext context)
    {
        Index = SH.FormatModelBindingHutaoComplexRankFloor(floor.Index);
        IndexValue = floor.Index;
        Disorders = floor.Descriptions.EmptyIfDefault();
        UpDisorders = floor.FirstDescriptions.EmptyIfDefault();
        DownDisorders = floor.SecondDescriptions.EmptyIfDefault();

        Levels = [.. context.IdArrayTowerLevelMap[floor.LevelGroupId].OrderBy(l => l.Index).Select(l => LevelView.Create(l, context))];
    }

    [ObservableProperty]
    public partial bool Engaged { get; private set; }

    public string Index { get; }

    [ObservableProperty]
    public partial string? SettleTime { get; private set; }

    [ObservableProperty]
    public partial int Star { get; private set; }

    public ImmutableArray<string> Disorders { get; }

    public ImmutableArray<string> UpDisorders { get; }

    public ImmutableArray<string> DownDisorders { get; }

    public ImmutableArray<LevelView> Levels { get; }

    public uint IndexValue { get; }

    public static FloorView Create(TowerFloor floor, SpiralAbyssMetadataContext context)
    {
        return new(floor, context);
    }

    public void Attach(Web.Hoyolab.Takumi.GameRecord.SpiralAbyss.SpiralAbyssFloor floor, TimeSpan offset, SpiralAbyssMetadataContext context)
    {
        SettleTime = $"{DateTimeOffset.FromUnixTimeSeconds(floor.SettleTime).ToOffset(offset):yyyy.MM.dd HH:mm:ss}";
        Star = floor.Star;
        Engaged = true;

        foreach (ref readonly LevelView levelView in Levels.AsSpan())
        {
            uint levelIndex = levelView.IndexValue;
            if (floor.Levels.SingleOrDefault(l => l.Index == levelIndex) is { } level)
            {
                levelView.Attach(level, offset, context);
            }
        }
    }
}
