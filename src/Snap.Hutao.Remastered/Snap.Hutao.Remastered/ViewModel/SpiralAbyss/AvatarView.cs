// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model;

namespace Snap.Hutao.Remastered.ViewModel.SpiralAbyss;

public partial class AvatarView : ObservableObject, INameIconSide<Uri>
{
    protected AvatarView(Avatar metaAvatar)
    {
        Name = metaAvatar.Name;
        Icon = Model.Metadata.Converter.AvatarIconConverter.IconNameToUri(metaAvatar.Icon);
        SideIcon = Model.Metadata.Converter.AvatarIconConverter.IconNameToUri(metaAvatar.SideIcon);
        Quality = metaAvatar.Quality;
    }

    public string Name { get; private set => SetProperty(ref field, value); }

    public Uri Icon { get; private set => SetProperty(ref field, value); }

    public Uri SideIcon { get; private set => SetProperty(ref field, value); }

    public QualityType Quality { get; private set => SetProperty(ref field, value); }

    public static AvatarView From(Avatar source)
    {
        return new(source);
    }
}
