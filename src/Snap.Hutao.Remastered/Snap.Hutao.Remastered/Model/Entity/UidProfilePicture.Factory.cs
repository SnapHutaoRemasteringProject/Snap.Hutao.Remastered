// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Enka.Model;
using Snap.Hutao.Remastered.Web.Hoyolab;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

public sealed partial class UidProfilePicture
{
    public static UidProfilePicture From(PlayerUid uid, ProfilePicture profilePicture)
    {
        return new()
        {
            Uid = uid.ToString(),
            ProfilePictureId = profilePicture.Id,
            AvatarId = profilePicture.AvatarId,
            CostumeId = profilePicture.CostumeId,
            RefreshTime = DateTimeOffset.Now,
        };
    }
}
