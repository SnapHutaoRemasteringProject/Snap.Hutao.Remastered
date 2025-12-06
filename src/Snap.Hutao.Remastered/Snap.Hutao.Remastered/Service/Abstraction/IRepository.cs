// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Abstraction;

internal interface IRepository<TEntity> : IAppInfrastructureService
    where TEntity : class;