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
    private ImageSource? nameCardSource;

    public TileBackgroundAvatarItem(string uid, string avatarId, string name, Uri? nameCard, Uri? cachedPreview = null)
    {
        Uid = uid;
        AvatarId = avatarId;
        Name = name;
        NameCard = nameCard;

        // 已下载过该角色的背景图时优先用它：本地加载不受网络影响，
        // 而且它就是磁贴实际显示的内容（已按 31:15 裁切）。
        PreviewUri = cachedPreview ?? nameCard;
    }

    public string Uid { get; }

    public string AvatarId { get; }

    public string Name { get; }

    /// <summary>
    /// 名刺图片地址；「无背景」占位项为 <see langword="null"/>。
    /// </summary>
    public Uri? NameCard { get; }

    /// <summary>
    /// 预览图地址：优先为已缓存的本地背景图，否则退回远程名刺地址。
    /// </summary>
    public Uri? PreviewUri { get; }

    /// <summary>
    /// 预览图：「无背景」占位项为 <see langword="null"/>。
    /// 结果缓存在字段里：绑定会在每次属性变更时重新求值，
    /// 每次新建 <see cref="BitmapImage"/> 会让同一张图被反复下载。
    /// </summary>
    public ImageSource? NameCardSource
    {
        get
        {
            if (PreviewUri is null)
            {
                return null;
            }

            return nameCardSource ??= new BitmapImage(PreviewUri);
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
