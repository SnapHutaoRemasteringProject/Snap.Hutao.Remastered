// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.User;

[Service(ServiceLifetime.Singleton, typeof(IUidProfilePictureRepository))]
public sealed partial class UidProfilePictureRepository : IUidProfilePictureRepository
{
    [GeneratedConstructor]
    public partial UidProfilePictureRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public UidProfilePicture? SingleUidProfilePictureOrDefaultByUid(string uid)
    {
        try
        {
            return this.WithDatabase(context => AppDbQueries.ProfilePictureByUid(context, uid));
        }
        catch
        {
            this.WithDatabase(context => AppDbQueries.DeleteProfilePicturesByUid(context, uid));
            return default;
        }
    }

    public void UpdateUidProfilePicture(UidProfilePicture profilePicture)
    {
        this.Update(profilePicture);
    }

    public void DeleteUidProfilePictureByUid(string uid)
    {
        this.WithDatabase(context => AppDbQueries.DeleteProfilePicturesByUid(context, uid));
    }
}