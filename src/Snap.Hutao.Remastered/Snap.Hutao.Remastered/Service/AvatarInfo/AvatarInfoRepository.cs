// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;
using EntityAvatarInfo = Snap.Hutao.Remastered.Model.Entity.AvatarInfo;

namespace Snap.Hutao.Remastered.Service.AvatarInfo;

[Service(ServiceLifetime.Singleton, typeof(IAvatarInfoRepository))]
public sealed partial class AvatarInfoRepository : IAvatarInfoRepository
{
    [GeneratedConstructor]
    public partial AvatarInfoRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public ImmutableArray<EntityAvatarInfo> GetAvatarInfoImmutableArrayByUid(string uid)
    {
        return this.ImmutableArray<EntityAvatarInfo>(i => i.Uid == uid);
    }

    public void RemoveAvatarInfoRangeByUid(string uid)
    {
        this.Delete<EntityAvatarInfo>(i => i.Uid == uid);
    }

    public ImmutableArray<AvatarReliquaryScoreSetting> GetAvatarReliquaryScoreSettings()
    {
        return this.ImmutableArray<AvatarReliquaryScoreSetting>();
    }

    public void SaveAvatarReliquaryScoreSetting(AvatarReliquaryScoreSetting setting)
    {
        if (this.SingleOrDefault<AvatarReliquaryScoreSetting>(s => s.AvatarId == setting.AvatarId) is null)
        {
            this.Add(setting);
        }
        else
        {
            this.Update(setting);
        }
    }
}