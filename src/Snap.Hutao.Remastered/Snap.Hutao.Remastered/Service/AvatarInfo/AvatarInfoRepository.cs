// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;
using EntityAvatarInfo = Snap.Hutao.Remastered.Model.Entity.AvatarInfo;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.AvatarInfo;

[Service(ServiceLifetime.Singleton, typeof(IAvatarInfoRepository))]
public sealed partial class AvatarInfoRepository : IAvatarInfoRepository
{
    [GeneratedConstructor]
    public partial AvatarInfoRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public ImmutableArray<EntityAvatarInfo> GetAvatarInfoImmutableArrayByUid(string uid)
    {
        return this.WithDatabase(context => AppDbQueries.AvatarInfosByUid(context, uid)).ToImmutableArray();
    }

    public void RemoveAvatarInfoRangeByUid(string uid)
    {
        this.WithDatabase(context => AppDbQueries.DeleteAvatarInfosByUid(context, uid));
    }

    public ImmutableArray<AvatarReliquaryScoreSetting> GetAvatarReliquaryScoreSettings()
    {
        return this.ImmutableArray<AvatarReliquaryScoreSetting>();
    }

    public void SaveAvatarReliquaryScoreSetting(AvatarReliquaryScoreSetting setting)
    {
        if (this.WithDatabase(context => AppDbQueries.AvatarScoreSettingById(context, setting.AvatarId)) is null)
        {
            this.Add(setting);
        }
        else
        {
            this.Update(setting);
        }
    }
}