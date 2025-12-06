// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;

namespace Snap.Hutao.Remastered.Service.User;

internal interface IUidProfilePictureRepository : IRepository<UidProfilePicture>
{
    UidProfilePicture? SingleUidProfilePictureOrDefaultByUid(string uid);

    void UpdateUidProfilePicture(UidProfilePicture profilePicture);

    void DeleteUidProfilePictureByUid(string uid);
}