// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Abstraction;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.Hutao;

[Service(ServiceLifetime.Singleton, typeof(IAvatarStrategyRepository))]
public sealed partial class AvatarStrategyRepository : IAvatarStrategyRepository
{
    [GeneratedConstructor]
    public partial AvatarStrategyRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public AvatarStrategy? GetStrategyByAvatarId(AvatarId avatarId)
    {
        return this.WithDatabase(context => AppDbQueries.AvatarStrategyById(context, (uint)avatarId));
    }

    public void AddStrategy(AvatarStrategy strategy)
    {
        this.Add(strategy);
    }

    public void RemoveStrategy(AvatarStrategy strategy)
    {
        this.Delete(strategy);
    }
}