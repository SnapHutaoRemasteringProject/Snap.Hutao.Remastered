// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Snap.Hutao.Remastered.ViewModel.DailyNote;

/// <summary>
/// 磁贴背景候选：某个账户的名刺，可作为开始菜单磁贴背景。
/// </summary>
public sealed partial class TileBackgroundAvatarItem : ObservableObject
{
    public TileBackgroundAvatarItem(string uid, string avatarId, string name, Uri? nameCard)
    {
        Uid = uid;
        AvatarId = avatarId;
        Name = name;
        NameCard = nameCard;
    }

    public string Uid { get; }

    public string AvatarId { get; }

    public string Name { get; }

    /// <summary>
    /// 名刺图片地址；「无背景」占位项为 <see langword="null"/>。
    /// </summary>
    public Uri? NameCard { get; }

    /// <summary>
    /// 无背景占位项没有名刺，因此没有预览图。
    /// </summary>
    public ImageSource? NameCardSource
    {
        get
        {
            return NameCard is null ? null : new BitmapImage(NameCard);
        }
    }

    public bool IsNone
    {
        get => string.IsNullOrEmpty(AvatarId);
    }

    public override string ToString()
    {
        return IsNone ? Name : $"{Name} ({Uid})";
    }
}
