// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;

namespace Snap.Hutao.Remastered.ViewModel.HardChallenge;

internal sealed class AvatarBling : AvatarView
{
    private AvatarBling(bool isPlus, Avatar metaAvatar)
        : base(metaAvatar)
    {
        IsPlus = isPlus;
    }

    public bool IsPlus { get; }

    public static AvatarBling Create(HardChallengeBlingAvatar avatar, HardChallengeMetadataContext context)
    {
        return new(avatar.IsPlus, context.GetAvatar(avatar.AvatarId));
    }

    public static AvatarBling Create(HardChallengeBlingAvatar avatar, Avatar metaAvatar)
    {
        return new(avatar.IsPlus, metaAvatar);
    }
}