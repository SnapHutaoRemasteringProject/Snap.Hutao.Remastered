// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed class ProfilePicture
{
    [ProtoMember(1)]
    public ProfilePictureId Id { get; init; }

    [ProtoMember(2)]
    public string Icon { get; init; }

    [ProtoMember(3)]
    public string Name { get; init; }

    [ProtoMember(4)]
    public ProfilePictureUnlockBy UnlockType { get; init; }

    /// <summary>
    /// <see cref="ProfilePictureUnlockBy.Item"/> -> <see cref="MaterialId"/>
    /// <br/>
    /// <see cref="ProfilePictureUnlockBy.Avatar"/> -> <see cref="AvatarId"/>
    /// <br/>
    /// <see cref="ProfilePictureUnlockBy.Costume"/> -> <see cref="CostumeId"/>
    /// <br/>
    /// <see cref="ProfilePictureUnlockBy.ParentQuest"/> -> <see cref="QuestId"/>
    /// </summary>
    [ProtoMember(5)]
    public uint UnlockParameter { get; init; }
}
