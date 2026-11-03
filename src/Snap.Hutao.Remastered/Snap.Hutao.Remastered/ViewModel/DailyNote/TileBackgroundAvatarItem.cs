// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Snap.Hutao.Remastered.ViewModel.DailyNote;

/// <summary>
/// 磁贴背景候选：某个账户的名刺，可作为开始菜单磁贴背景。
/// </summary>
[ObservableObject]
public sealed partial class TileBackgroundAvatarItem
{
    public string Uid { get; set; } = default!;

    public string AvatarId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public Uri NameCard { get; set; } = default!;

    public ImageSource? NameCardSource
    {
        get
        {
            return new BitmapImage(NameCard);
        }
    }

    public override string ToString()
    {
        return $"{Name} ({Uid})";
    }
}
