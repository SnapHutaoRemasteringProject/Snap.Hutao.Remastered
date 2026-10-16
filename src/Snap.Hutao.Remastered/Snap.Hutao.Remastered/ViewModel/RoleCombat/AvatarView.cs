// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

public partial class AvatarView : ObservableObject, INameIconSide<Uri>
{
    protected AvatarView(Avatar metaAvatar)
    {
        Name = metaAvatar.Name;
        Icon = Model.Metadata.Converter.AvatarIconConverter.IconNameToUri(metaAvatar.Icon);
        SideIcon = Model.Metadata.Converter.AvatarIconConverter.IconNameToUri(metaAvatar.SideIcon);
        Quality = metaAvatar.Quality;
    }

    private AvatarView(RoleCombatAvatar roleCombatAvatar, Avatar metaAvatar)
        : this(metaAvatar)
    {
        Type = roleCombatAvatar.AvatarType;
    }

    public string Name { get; private set => SetProperty(ref field, value); }

    public Uri Icon { get; private set => SetProperty(ref field, value); }

    public Uri SideIcon { get; private set => SetProperty(ref field, value); }

    public QualityType Quality { get; private set => SetProperty(ref field, value); }

    public RoleCombatAvatarType Type { get; private set => SetProperty(ref field, value); }

    public static AvatarView Create(Avatar metaAvatar)
    {
        return new(metaAvatar);
    }

    public static AvatarView Create(RoleCombatAvatar roleCombatAvatar, RoleCombatMetadataContext context)
    {
        return new(roleCombatAvatar, context.GetAvatar(roleCombatAvatar.AvatarId));
    }

    public static AvatarView Create(RoleCombatAvatar roleCombatAvatar, Avatar metaAvatar)
    {
        return new(roleCombatAvatar, metaAvatar);
    }
}
