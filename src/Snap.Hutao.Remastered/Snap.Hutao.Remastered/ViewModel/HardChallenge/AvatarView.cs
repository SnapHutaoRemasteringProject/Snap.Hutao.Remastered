// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;

namespace Snap.Hutao.Remastered.ViewModel.HardChallenge;

public partial class AvatarView : ObservableObject, INameIconSide<Uri>
{
    protected AvatarView(Avatar metaAvatar)
    {
        Name = metaAvatar.Name;
        Icon = Model.Metadata.Converter.AvatarIconConverter.IconNameToUri(metaAvatar.Icon);
        SideIcon = Model.Metadata.Converter.AvatarIconConverter.IconNameToUri(metaAvatar.SideIcon);
        Quality = metaAvatar.Quality;
    }

    private AvatarView(HardChallengeAvatar avatar, Avatar metaAvatar)
        : this(metaAvatar)
    {
        ActivatedConstellationCount = avatar.Rank;
    }

    private AvatarView(HardChallengeSimpleAvatar avatar, Avatar metaAvatar)
        : this(metaAvatar)
    {
    }

    public string Name { get; private set => SetProperty(ref field, value); }

    public Uri Icon { get; private set => SetProperty(ref field, value); }

    public Uri SideIcon { get; private set => SetProperty(ref field, value); }

    public QualityType Quality { get; private set => SetProperty(ref field, value); }

    public int ActivatedConstellationCount { get; init => SetProperty(ref field, value); }

    public static AvatarView Create(Avatar metaAvatar)
    {
        return new(metaAvatar);
    }

    public static AvatarView Create(HardChallengeAvatar avatar, HardChallengeMetadataContext context)
    {
        return new(avatar, context.GetAvatar(avatar.AvatarId));
    }

    public static AvatarView Create(HardChallengeSimpleAvatar avatar, HardChallengeMetadataContext context)
    {
        return new(avatar, context.GetAvatar(avatar.AvatarId));
    }

    public static AvatarView Create(HardChallengeAvatar avatar, Avatar metaAvatar)
    {
        return new(avatar, metaAvatar);
    }
}
