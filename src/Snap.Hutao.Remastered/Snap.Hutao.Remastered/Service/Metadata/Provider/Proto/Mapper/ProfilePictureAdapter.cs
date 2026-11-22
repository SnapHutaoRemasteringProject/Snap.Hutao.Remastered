// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoProfilePicture = Snap.Hutao.Remastered.Metadata.ProfilePicture;
using ProtoProfilePictureUnlockBy = Snap.Hutao.Remastered.Metadata.ProfilePictureUnlockBy;
using ProfilePictureUnlockBy = Snap.Hutao.Remastered.Model.Intrinsic.ProfilePictureUnlockBy;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class ProfilePictureAdapter : ProtoCategoryAdapter<ProtoProfilePicture, Model.Metadata.Avatar.ProfilePicture>
{
    public ProfilePictureAdapter()
        : base(MetadataCategories.ProfilePicture)
    {
    }

    public override IEnumerable<ProtoProfilePicture> Enumerate(LanguageMetadata language)
    {
        return language.ProfilePictures;
    }

    public override Model.Metadata.Avatar.ProfilePicture Map(ProtoProfilePicture message, MetadataConvert convert)
    {
        return new()
        {
            Id = (ProfilePictureId)message.Id,
            Icon = convert.Text(message.Icon),
            Name = convert.Text(message.Name),
            UnlockType = convert.ToEnum<ProtoProfilePictureUnlockBy, ProfilePictureUnlockBy>(message.UnlockType, nameof(message.UnlockType)),
            UnlockParameter = message.UnlockParameter,
        };
    }
}
